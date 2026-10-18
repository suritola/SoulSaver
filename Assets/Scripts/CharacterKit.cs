using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 거너가 아닌 캐릭터: 스탯 배율 · 새 몸 그림(대기/달리기) · 평타 · 우클릭 스킬
// PlayerController 가 평타 · 우클릭 · 이동을 이 컴포넌트에 맡김 (거너면 붙지 않음)
public partial class CharacterKit : MonoBehaviour
{
    public static CharacterKit Instance { get; private set; }

    public CharacterId Id { get; private set; }
    CharacterDef def;
    PlayerController player;
    SpriteRenderer body;
    Sprite[] frames;
    float animT;
    Vector3 lastPos;

    // 누르고 있는 우클릭 (검사 회전 베기 · 연금술사 대폭발)
    bool charging;
    float charge;
    LineRenderer ring;

    // 도적 출혈 돌진 (돌진하는 동안은 평타 · 이동 입력을 막음)
    float dashUntil;
    public bool Dashing => Time.time < dashUntil;

    // 검사 평타: 위→아래, 아래→위 번갈아 휘두름
    bool swingAlt;

    public bool Busy => charging || Dashing;
    // 우클릭을 모으는 중 (고유 스킬: 강철 의지 · 유리 비 · 굳건한 자세)
    // 무기 진화 뒤에는 진화 무기 필살기를 꾹 눌러 조준하는 동안도 같게 셈 (회전 베기 · 대폭발이 없어져도 카드가 쓸모 있게)
    public bool Charging => charging;
    public float AttackSpeedMul => FormRate * SignatureSkills.AttackRateMul;          // 진화 형태에 따라 (전쟁 망치는 느리게, 독침 대롱은 빠르게)
    // 궁수: 시위를 당기는 동안 40% 느려짐 (1.0.1: 쏘면서 마음대로 도망칠 수 있어 위험이 없었음)
    // 1.0.4~: 유령이 빙의해 적의 몸을 끌고 다니는 동안은 그 적의 이동 속도 그대로 (CharacterKit.Ghost · PossessMoveMul)
    public float MoveMul => possessSpeed >= 0f ? PossessMoveMul() : DrawsBow && drawing ? 0.6f : 1f;
    public string WeaponName => Loc.T(def.weapon);
    // 탄창이 있는 캐릭터 (도적 표창 6발): 다 쓰면 거너처럼 재장전
    public bool UsesAmmo => def.mag > 0;

    // 궁수 활 시위: 누르고 있는 동안 draw 0 → 1
    public bool DrawsBow => Id == CharacterId.Archer;
    public bool BowFired { get; private set; }
    public float Draw => drawing ? draw : 0f;
    bool drawing;
    float draw;
    LineRenderer bowLine;
    bool fullPlayed;
    // 1.0.1 완벽한 타이밍: 가득 찬 뒤 PerfectWindow 안에 놓으면 「완벽!」 300%, 너무 오래 쥐면 활이 떨리며 약해짐 (최저 160%)
    float fullAt;
    const float PerfectWindow = 0.5f, TrembleAfter = 0.9f;      // 1.0.1: 0.2초는 너무 빡빡했음
    const float DrawPower = 1.9f, PerfectPower = 3f, MinHeldPower = 1.6f;
    const float PierceFalloff = 0.85f;       // 꿰뚫을 때마다 피해 -15% (일렬 쓸어 버리기가 너무 셌음)

    // 캐릭터 전용 레벨업 카드 4장의 레벨 (LevelShop.Kits, 0 = 아직 없음). 기본 스펙이 아닌 특수 능력
    //   검사: 0 검기 · 1 흡혈 베기 · 2 쳐내기 · 3 칼바람
    //   도적: 0 도탄 표창 · 1 갈고리 표창 · 2 그림자 분신 · 3 표창 폭풍
    //   궁수: 0 분열 화살 · 1 메아리 화살 · 2 강풍 화살 · 3 가시 덤불
    //   연금술사: 0 연쇄 반응 · 1 급속 냉동 · 2 호문쿨루스 · 3 파편 플라스크
    //   도적만 4: 사냥의 기세 (처치할 때마다 스킬 게이지)
    //   5 · 6 (레벨업 12 · 13번 칸): 검사 굳건한 자세 · 연속 베기 / 도적 급소 노리기 · 표창 회수
    //                               궁수 정조준 · 사냥감 표식 / 연금술사 원소 융합 · 끈적한 산성
    public readonly int[] card = new int[7];

    // 캐릭터 능력치 상점이 올리는 값 (Shop.Kits)
    [HideInInspector] public float reachMul = 1f;     // 검사 긴 칼날: 베기 사거리 · 손에 든 검 크기
    [HideInInspector] public float arcBonus;          // 검사 넓은 베기: 부채꼴 반각 +
    [HideInInspector] public float ultMul = 1f;       // 우클릭 강화 (회전 베기 · 출혈 돌진 · 화살비 · 대폭발)
    // 스킬 강화 상점의 첫 줄(캐릭터 우클릭) 위력 · 효과 범위까지 곱한 값
    float UltMul => ultMul * (Special != null ? Special.UltPower(SpecialAbilities.PistolUlt) * (1f + 0.1f * Special.UltTrait(SpecialAbilities.PistolUlt)) : 1f);
    [HideInInspector] public float blastMul = 1f;     // 연금술사 넓은 폭발: 평타 폭발 범위

    public static void Attach(PlayerController p)
    {
        if (p == null || CharacterData.IsGunner || p.GetComponent<CharacterKit>() != null) return;
        p.gameObject.AddComponent<CharacterKit>();
    }

    void Awake()
    {
        Instance = this;
        Id = CharacterData.Selected;
        def = CharacterData.Current;
        player = GetComponent<PlayerController>();
        body = GetComponent<SpriteRenderer>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EnermyController.Killed -= OnKill;
        if (homunculus != null) Destroy(homunculus);
        if (reagentTag != null) Destroy(reagentTag);
        if (ultAimLine != null) Destroy(ultAimLine.gameObject);
        if (ultAimRing != null) Destroy(ultAimRing.gameObject);
        OnDestroyCartographer();
        OnDestroyClockmaker();
        OnDestroyPostman();
        OnDestroyGhost();
        OnDestroyRules();
    }

    void Start()
    {
        // 거너 기준 스탯 배율
        player.PlayerMaxHealth *= def.hp;
        player.PlayerHealth = player.PlayerMaxHealth;
        player.damage *= def.damage;
        baseDamage = player.damage;
        EnermyController.Killed += OnKill;
        player.ShootSpeed /= Mathf.Max(0.1f, def.attackSpeed);
        player.speed *= def.move;
        if (def.mag > 0) player.MaxBullet = def.mag;
        if (Id == CharacterId.Rogue) player.reloadTime = 1f;      // 표창은 빨리 다시 쥠          // (거너의 mag 는 표시용, 실제 탄창은 인스펙터 값)
        player.NowBullet = player.MaxBullet;

        // 새 몸 그림 (애니메이터는 거너 그림을 쓰므로 끔)
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
        List<Sprite> list = new List<Sprite>(Resources.LoadAll<Sprite>("Characters/" + SkinData.BodySheet(def.body)));     // 캐릭터 스킨
        if (list.Count == 0) list.AddRange(Resources.LoadAll<Sprite>("Characters/" + def.body));
        list.Sort((a, b) => Index(a.name).CompareTo(Index(b.name)));
        frames = list.ToArray();
        if (frames.Length > 0) body.sprite = frames[0];
        lastPos = transform.position;
    }

    static int Index(string n)
    {
        int i = n.LastIndexOf('_');
        return i >= 0 && int.TryParse(n.Substring(i + 1), out int v) ? v : 0;
    }

    // 코루틴도 시작할 때의 피해 출처(평타 · 필살기)를 이어 감 (RunStats)
    public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(DamageSource.Keep(routine));

    void LateUpdate()
    {
        // 멈춰 있던 시간 (몸 그림 장수와 상관없이 늘 셈 — 예전엔 그림이 6장 아래면 0에 멈춰 정조준이 안 됐음)
        bool moving = (transform.position - lastPos).sqrMagnitude > 0.0004f && Time.timeScale > 0f;
        stillTime = moving ? 0f : stillTime + Time.deltaTime;
        // 대기 2장 (느리게) · 달리기 4장
        if (frames != null && frames.Length >= 6)
        {
            animT += Time.deltaTime;
            body.sprite = moving ? frames[2 + (int)(animT * 10f) % 4] : frames[(int)(animT * 2f) % 2];
        }
        lastPos = transform.position;
        UpdateReagentTag();
        UpdateKitRules();       // 궁수 숨 · 연금술사 불안정도 (CharacterKit.Rules)
    }

    // ================================================================= 연금술사: 다음 플라스크 시약 표시
    // 머리 위에 다음에 던질 플라스크(시약 색) + 이름. 현자의 돌 · 과부하로 반드시 불안정해지면 보라색 "불안정"
    GameObject reagentTag;
    SpriteRenderer reagentIcon;
    TMPro.TextMeshPro reagentText;
    int shownReagent = -1;
    Loc.Lang shownReagentLang;

    void UpdateReagentTag()
    {
        if (Id != CharacterId.Alchemist || body == null) return;
        if (reagentTag == null && !BuildReagentTag()) return;

        bool unstable = (Special != null && Special.KitStoneActive) || NextForcedUnstable;
        int show = unstable ? 3 : reagent;
        if (show != shownReagent || shownReagentLang != Loc.Current)
        {
            shownReagent = show;
            shownReagentLang = Loc.Current;
            Color c = unstable ? new Color(0.85f, 0.4f, 1f) : ReagentColors[reagent];
            reagentIcon.color = c;
            reagentText.text = Loc.T(unstable ? "불안정 시약" : NextReagentName);
            reagentText.color = Color.Lerp(c, Color.white, 0.35f);
            // 글자 왼쪽에 플라스크 그림 (둘을 합쳐 가운데 정렬)
            float w = reagentText.preferredWidth;
            reagentText.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            reagentIcon.transform.localPosition = new Vector3(0.6f - w * 0.5f - 0.8f, 0.05f, 0f);
        }
        float top = body.bounds.max.y;
        reagentTag.transform.position = new Vector3(transform.position.x, top + 1f + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
    }

    bool BuildReagentTag()
    {
        Sprite[] f = Fx.Frames("fx_flask");
        if (f == null || f.Length == 0) return false;
        reagentTag = new GameObject("ReagentTag");

        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(reagentTag.transform, false);
        reagentIcon = icon.AddComponent<SpriteRenderer>();
        reagentIcon.sprite = f[0];
        reagentIcon.sortingLayerID = PlayerLook.TopLayer;       // 1.0.4~ 머리 위 표시도 플레이어와 같이 모든 이펙트 위
        reagentIcon.sortingOrder = 40;
        icon.transform.localScale = Vector3.one * (1.3f / f[0].bounds.size.y);

        GameObject label = new GameObject("Label", typeof(TMPro.TextMeshPro));
        label.transform.SetParent(reagentTag.transform, false);
        reagentText = label.GetComponent<TMPro.TextMeshPro>();
        UIKit.EnsureStyle();
        if (UIKit.Font != null) reagentText.font = UIKit.Font;
        if (UIKit.FontMaterial != null) reagentText.fontSharedMaterial = UIKit.FontMaterial;
        reagentText.fontSize = 8f;          // 플레이어 몸(약 3칸) 폭에 맞는 크기
        reagentText.fontStyle = TMPro.FontStyles.Bold;
        reagentText.alignment = TMPro.TextAlignmentOptions.Center;
        reagentText.enableWordWrapping = false;
        reagentText.outlineWidth = 0.3f;
        reagentText.outlineColor = new Color32(0, 0, 0, 255);
        reagentText.rectTransform.sizeDelta = new Vector2(16f, 2.5f);
        reagentText.sortingLayerID = PlayerLook.TopLayer;
        reagentText.sortingOrder = 41;
        shownReagent = -1;
        return true;
    }

    // ================================================================= 공통
    SpecialAbilities Special => player.special;
    float Damage => player.damage * player.damageMultiplier;
    Vector3 Mouse
    {
        get
        {
            if (Special != null) return Special.MouseWorldPos;
            Vector3 m = Camera.main.ScreenToWorldPoint(GameInput.MousePosition);
            m.z = 0f;
            return m;
        }
    }

    static void Play(string sound, float vol = 1f, float pitch = 1f) => Hostile.Play(sound, vol, pitch);

    // 반지름 안의 적 · 보스 모두에게
    //   weakMul > 0: 이 점이 빈틈 위면 그 보스 · 중간 보스만 배율 (연금술사 첫 병 직격점)
    public static int DamageCircle(Vector3 pos, float radius, float damage, float knock, float weakMul = 0f)
    {
        int n = 0;
        using (var ovl = PhysHits.Circle(pos, radius)) foreach (Collider2D c in ovl)
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (weakMul > 1f) WeakHit.Hit(c.gameObject, WeakShape.Point(pos, 0.5f, weakMul), damage, (c.transform.position - pos).normalized, knock);
            else Specials.Damage(c.gameObject, damage, (c.transform.position - pos).normalized, knock);
            n++;
        }
        return n;
    }

    // 도트 이펙트를 입힌 투사체 (Bullet 판정 그대로)
    Bullet Projectile(Vector3 start, Vector2 dir, float damage, int pene, float speed, float range, string fx, float size, Color tint, bool spin)
    {
        Bullet b = player.CreateBullet(start, dir, damage, pene, player.getHP, false);
        if (b == null) return null;
        b.speed = speed;
        b.lifetime = range > 0f ? range / speed : 6f;
        SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
        Sprite[] f = Fx.Frames(fx);
        if (sr != null && f.Length > 0)
        {
            // 1.1.2: 평타를 한눈에 (AttackLook) — 보이는 크기는 예전의 0.9~1.2배(검기 0.6~0.78배, 큰 톱니 0.72~0.9배)
            //   표창 · 화살 · 톱니는 새 그림(흰 심 + 영웅색 테두리 + 어두운 외곽선), 나머지는 예전 그림을 키움
            //   판정 = 보이는 그림의 몸통 (AttackLook.BodyOf: 외곽선 · 빛 무리 · 잔상 뺌), 표창 · 톱니는 원
            int tier = AttackLook.Tier;
            bool wave = fx == "fx_swordwave";
            string look = AttackLook.LookFor(fx, Id == CharacterId.Clockmaker);
            Sprite[] lf = look != null ? Fx.Frames(look) : null;
            if (lf == null || lf.Length == 0) { lf = f; look = null; }
            float vis = size * (wave ? AttackLook.WaveMul(tier) : size >= 1.8f ? AttackLook.BigMul(tier) : AttackLook.SmallMul(tier));
            sr.sprite = lf[0];
            sr.color = tint;
            AttackLook.Shape(b, lf[0], look ?? fx, vis);
            FrameLoop loop = b.gameObject.AddComponent<FrameLoop>();
            loop.frames = lf;
            loop.spin = spin ? -900f : 0f;
            if (wave) SlashTrail.Attach(b);      // 검기: 총알 빛 대신 초승달 잔상 (1.0.1)
            else if (b.TryGetComponent(out BulletGlow glow))
            {
                glow.styled = true;
                glow.frames = lf;
                if (look != null) glow.coreFrames = Fx.Frames(look + "_core");
            }
        }
        return b;
    }

    IEnumerable<Vector2> Spread(Vector2 dir, int shots, float step)
    {
        for (int i = 0; i < shots; i++)
            yield return Quaternion.Euler(0f, 0f, (i - (shots - 1) * 0.5f) * step) * dir;
    }

    // ================================================================= 평타
    public void Attack()
    {
        using var aim = WeakHit.AimScope(1.75f);       // 빈틈: 평타는 직접 조준 (WeakSpot · 1-3 표)
        Vector3 target = Mouse;
        player.FaceTowards(target);
        Vector3 start = player.MuzzlePosition;
        Vector2 dir = ((Vector2)(target - start)).normalized;
        int shots = Mathf.Max(1, player.multiShot);
        float dmg = Damage * player.MultiShotDamageRate(shots);
        // 그림자 은신: 오래 안 맞았으면 이번 평타가 강해짐
        float veil = Special != null ? Special.KitConsumeVeil() : 1f;
        dmg *= veil;
        PlayerLook.Fired(-1);
        // 2026-10-19~ 평타 모양을 바꾸는 갈래 입문 (Paths.<영웅>.KitAttack 이 true 면 원래 평타 대신)
        if (Paths.KitAttack(this, start, dir, dmg, shots)) return;

        switch (Id)
        {
            case CharacterId.Swordsman:
                Swing(dir, shots);
                break;
            case CharacterId.Rogue:
                RogueThrow(start, dir, dmg, shots);          // 진화 형태 · 강화 (CharacterKit.Forms)
                SignatureSkills.Attacked(start, dir);          // 회전 표창 · 그림자 표창
                break;
            case CharacterId.Archer:
                foreach (Vector2 d in Spread(dir, shots, 6f))
                    Projectile(start, d, dmg, player.pene + 1, 60f, 0f, "fx_arrow", 0.45f, Color.white, false);
                Play("pew", 0.5f, 0.7f);
                break;
            case CharacterId.Alchemist:
                for (int i = 0; i < shots; i++)
                {
                    Vector3 land = ClampRange(start, target, def.range) + (i == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle * 1.6f));
                    ThrowReagent(start, land, dmg, 0, i == 0 && WeakHit.InAim ? 1.6f : 0f);     // 빈틈: 커서 자리로 던진 첫 병의 직격점만
                }
                Play("glassclink", 0.6f, Random.Range(0.9f, 1.15f));
                Play("whoosh", 0.25f, 1.5f);
                break;
            case CharacterId.Cartographer:
                MapAttack(start, dir, dmg, shots);          // 잉크 획 (CharacterKit.Cartographer)
                break;
            case CharacterId.Clockmaker:
                ClockAttack(start, dir, dmg, shots);        // 톱니 투척 (CharacterKit.Clockmaker)
                break;
            case CharacterId.Postman:
                PostAttack(start, dir, dmg, shots);         // 편지 배달 (CharacterKit.Postman)
                break;
            case CharacterId.Ghost:
                GhostAttack(start, dir, dmg, shots);        // 차가운 손길 (CharacterKit.Ghost)
                break;
        }
    }

    // 검사 평타: 장검을 크게 휘둘러 앞쪽 부채꼴(반지름 = 사거리)의 적을 직접 벰
    // 여러 발 강화를 얻으면 휘두르는 폭이 넓어짐
    void Swing(Vector2 dir, int shots)
    {
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        if (LanceIntercept(dir, shots)) return;          // 기창: 먼저 돌진하고 도착해서 찌름
        // 연속 베기: 네 번째(2레벨부터 세 번째) 베기마다 더 멀리 · 두 배로
        bool combo = false;
        if (card[6] > 0)
        {
            comboCount++;
            if (comboCount >= (card[6] >= 2 ? 3 : 4)) { combo = true; comboCount = 0; }
        }
        if (SignatureSkills.ForceCombo) combo = true;           // 광검무
        float reach = def.range * reachMul * (combo ? 1.4f : 1f) * SwingReachMul;
        float half = SwingHalf(Mathf.Min(180f, 50f + arcBonus + 12f * (shots - 1)));
        SwingFormBefore(dir);
        swingAlt = !swingAlt;
        bool left = dir.x < 0f;
        PlayerLook.Swing(swingAlt);

        Vector3 origin = transform.position;
        float sig = SignatureSkills.BeforeSwing(origin, dir);      // 영혼 흡수 · 반격 검기
        int hits = 0;
        using (var ovl = PhysHits.Circle(origin, reach)) foreach (Collider2D c in ovl)
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            Vector2 to = c.transform.position - origin;
            if (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > half) continue;
            // 근접 특성: 벤 적을 크게 밀쳐내 몸에 닿기 어렵게
            // 빈틈: 부채꼴 전체가 아니라 휘두른 방향 가운데 20° 안 (WeakHit.Damage 는 조준 범위 밖이면 그냥 피해)
            WeakHit.Damage(c.gameObject, WeakShape.Arc(origin, dir, 10f, reach, 1.4f), Damage * sig * (combo ? 1.6f : 1f) * SwingHitMul(c, to.magnitude, reach)     /* 1.0.1: 연속 베기 2 → 1.6배 */, to.normalized, combo ? 4f : 3f);
            OnSwingHit(c, hits == 0);
            SignatureSkills.SwingHit(c);
            Fx.Spawn("fx_sparkle", c.transform.position, 0.9f, new Color(0.8f, 0.9f, 1f), 24f);
            hits++;
        }
        SwingCards(origin, dir, reach, half, hits);
        SwingFormAfter(origin, reach);
        SignatureSkills.AfterSwing(origin, dir, reach, hits);

        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // 1.1.2: 굵은 칼날 그림(흰 날 · 하늘색 테 · 어두운 외곽선, make_attacks.py) — 바깥 크기 = 판정 반지름 그대로
        //   적 · 맵 물체 · 적 이펙트 위 순서 (AttackLook.AreaOrder), 연속 베기는 색을 곱해 탁해지지 않게 옅은 금빛
        FxAnim a = Fx.Play("fx_swordswing", origin, reach * 2.03f, SkinFx.Tint(combo ? new Color(1f, 0.95f, 0.75f, 1f) : new Color(1f, 1f, 1f, 0.95f)), 24f, rot, AttackLook.AreaOrder);
        if (combo)
        {
            Play("crack", 0.6f, 0.9f);
            Hostile.Shake(0.1f);
            // 3레벨: 마무리 베기에 충격파
            if (card[6] >= 3)
            {
                DamageCircle(origin, reach, Damage, 2f);
                Fx.Spawn("fx_shock", origin, reach * 2f, new Color(1f, 0.85f, 0.45f, 0.8f), 20f);
            }
        }
        if (a != null) a.sr.flipY = left ^ !swingAlt;
        if (half > 52f)
        {
            // 넓어진 폭: 양옆으로 한 번씩 더 그림
            FxAnim b = Fx.Play("fx_swordswing", origin, reach * 2.03f, SkinFx.Tint(new Color(0.85f, 0.95f, 1f, 0.6f)), 24f, rot + (half - 50f), AttackLook.AreaOrder - 1);
            FxAnim c = Fx.Play("fx_swordswing", origin, reach * 2.03f, SkinFx.Tint(new Color(0.85f, 0.95f, 1f, 0.6f)), 24f, rot - (half - 50f), AttackLook.AreaOrder - 1);
            if (b != null) b.sr.flipY = a != null && a.sr.flipY;
            if (c != null) c.sr.flipY = a != null && a.sr.flipY;
        }
        Play("slash", 0.8f, hits > 0 ? 0.9f : 1.15f);
        Play("whoosh", 0.5f, 0.8f);
        if (hits >= 3) Hostile.Shake(0.08f);
    }

    // canDraw: 지금 당길 수 있는지 (전용 무기 · 우클릭 중이면 아님), fullTime: 가득 당기는 데 걸리는 시간
    int perfectRun;                     // 1.2~ 「완벽!」 연달아 (영혼 계약)
    // 트레일러 촬영용: 자동 조종에서도 손으로 놓는 활 (가득 차도 저절로 쏘지 않아 「완벽!」이 나올 수 있음 · 촬영 도구가 리플렉션으로 켬)
    public static bool ManualBowInAuto;

    public void UpdateBow(bool canDraw, float fullTime)
    {
        BowFired = false;
        if (!drawing)
        {
            if (canDraw && GameInput.FireHeld)
            {
                drawing = true;
                draw = 0f;
                fullPlayed = false;
                Play("bowdraw", 0.45f, 1f);
            }
            else
            {
                SpecialFeedback.Hide(bowLine);
                return;
            }
        }

        // 사냥꾼의 집중: 순식간에 가득 당김
        if (Special != null && (Special.KitFocusActive || Special.KitFreeDraws > 0)) fullTime = 0.05f;
        else if (SignatureSkills.InstantDraw) fullTime = 0.05f;          // 폭풍의 길: 바람길 위
        // 정조준: 늘 더 빨리 가득 당김 (예전 「가만히 서서」 조건은 없앰)
        else if (card[5] > 0) fullTime /= 1f + 0.25f * card[5];
        // 순식간에 가득 당기는 버프 · 자동 조작은 예전처럼 가득 차면 저절로 쏨
        // 1.1.9~ 설정 「공격 고정」이 걸려 있으면 가득 당겨진 순간 저절로 놓음 (「완벽!」 보너스 없음 · 숨 규칙은 그대로)
        bool lockRelease = GameInput.AttackLockNow && !GameInput.FireKeyHeld;
        bool autoRelease = fullTime <= 0.05f || ((GameInput.Auto || GameInput.TrailerRunning) && !ManualBowInAuto) || lockRelease;
        draw = Mathf.Min(1f, draw + Time.deltaTime / Mathf.Max(0.05f, fullTime));
        if (!GameInput.Auto && !GameInput.TrailerRunning) ConsumeBreath();       // 숨 고르기: 당기는 동안 숨이 닳음
        float heldFull = draw >= 1f && fullPlayed ? Time.time - fullAt : 0f;
        Vector3 start = player.MuzzlePosition;
        Vector3 target = Mouse;
        player.FaceTowards(target);
        Vector2 dir = ((Vector2)(target - start)).normalized;
        // 너무 오래 쥐고 있으면 활이 떨림
        if (!autoRelease && heldFull > TrembleAfter)
            dir = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 38f) * Mathf.Min(4f, (heldFull - TrembleAfter) * 4f)) * dir;
        else if (Breathless)
            dir = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 30f) * 3f) * dir;          // 숨이 차서 떨림
        PlayerLook.Draw(draw);

        // 겨누는 선: 시위를 당기는 동안만, 화면 끝까지 닿는 점선 (당길수록 밝고 굵게, 가득 당기면 금색으로 반짝)
        bool inWindow = draw >= 1f && heldFull <= PerfectWindow && !Breathless;
        Color c = draw < 1f ? new Color(0.7f, 1f, 0.6f, 0.3f + 0.5f * draw)
                : inWindow ? new Color(1f, 0.95f, 0.55f, 1f)                                   // 지금 놓으면 완벽
                : heldFull > TrembleAfter && !autoRelease ? new Color(1f, 0.5f, 0.35f, 0.55f)   // 떨림: 약해지는 중
                : new Color(1f, 0.85f, 0.3f, 0.6f + 0.3f * Mathf.Sin(Time.time * 20f));
        SpecialFeedback sfx = SpecialAbilities.SharedFx;
        if (sfx != null && Special != null)
        {
            if (bowLine == null) bowLine = sfx.NewLine("BowDraw", true, 17);
            sfx.SetLine(bowLine, start, start + (Vector3)(dir * Special.ScreenEdgeDistance(start, dir)), c, 0.06f + 0.08f * draw + (inWindow && !autoRelease ? 0.08f : 0f));
        }
        if (draw >= 1f && !fullPlayed)
        {
            fullPlayed = true;
            fullAt = Time.time;
            Play("ding", 0.7f, 1.6f);                                          // 지금 놓으라는 신호
            Fx.Spawn("fx_sparkle", start, 1.6f, new Color(1f, 0.9f, 0.5f), 24f);
        }

        // 1.0.1: 가득 차도 저절로 쏘지 않음 — 놓는 순간이 중요 (버프 · 자동 조작은 예전처럼)
        if (GameInput.FireHeld && canDraw && (draw < 1f || !autoRelease)) return;

        // 발사: 피해 30% → 280%, 화살 속도 35 → 95 (연타보다 끝까지 당기는 쪽이 초당 피해가 높게)
        drawing = false;
        SpecialFeedback.Hide(bowLine);
        PlayerLook.Draw(0f);
        PlayerLook.Fired(-1);
        bool perfect = !autoRelease && draw >= 1f && heldFull <= PerfectWindow && !Breathless;
        float power = 0.3f + DrawPower * draw;                                  // 가득 당김 220%
        if (perfect) power = PerfectPower;
        else if (!autoRelease && draw >= 1f && heldFull > TrembleAfter)
            power = Mathf.Max(MinHeldPower, power - 0.4f * (heldFull - TrembleAfter));
        if (Breathless && (!autoRelease || lockRelease)) power *= 0.5f;         // 숨이 차면 절반 (공격 고정도)
        // 1.2~ 영혼 계약 · 보드: 「완벽!」 화살 · 연달아
        if (perfect) { RunCounters.Both("perfect"); perfectRun++; RunCounters.Max("perfect.streak", perfectRun); }
        else if (!autoRelease) perfectRun = 0;
        if (perfect)
        {
            RestoreBreath(PerfectBreath);                                       // 완벽한 사격은 숨을 되돌려 줌
            SpecialAbilities.SharedFx?.FloatText(start + Vector3.up * 1.4f, Loc.T("완벽!"), new Color(1f, 0.9f, 0.4f), 4.5f, 0f);
            Fx.Spawn("fx_sparkle", start, 2.2f, Color.white, 26f);
            Play("chime", 0.5f, 1.8f);
        }
        float speed = 35f + 60f * draw;
        int shots = Mathf.Max(1, player.multiShot);
        float dmg = Damage * player.MultiShotDamageRate(shots) * power;
        using (WeakHit.AimScope(1.75f))         // 빈틈: 시위를 놓는 순간 커서 쪽 화살 (완벽! 은 ×2.1)
        foreach (Vector2 d in Spread(dir, shots, 6f))
        {
            Bullet b = Projectile(start, d, dmg, player.pene + 1, speed, 0f, "fx_arrow", 0.4f + 0.25f * draw + (perfect ? 0.15f : 0f), perfect ? new Color(1f, 0.8f, 0.25f) : draw >= 1f ? new Color(1f, 0.95f, 0.6f) : Color.white, false);
            if (b == null) continue;
            b.perfect = perfect;
            ArrowForm(b, draw, start);
            if (draw >= 1f) b.pene += 1;                        // 가득 당기면 하나 더 꿰뚫음
            if (card[0] > 0 && draw >= 1f) b.onHitEnemy += (arrow, c) => SplitArrow(arrow, c);
            if (card[6] > 0 && draw >= 1f) b.onHitEnemy += (arrow, c) => HuntMark.Apply(c.gameObject, 4f);
            if (card[2] > 0 && draw >= 1f) GustArrow(b);
            SignatureSkills.Arrow(b, draw);
            b.onHitEnemy += (arrow, col) => arrow.damage *= PierceFalloff;     // 꿰뚫을 때마다 -15% (맨 마지막에: 다른 효과는 맞힌 그 피해로)
        }
        SignatureSkills.BowRelease(start, dir, draw);
        if (draw >= 1f && Special != null && Special.KitFreeDraws > 0) Special.KitFreeDraws--;
        if (card[1] > 0) StartCoroutine(EchoArrow(dir, dmg * 0.5f, speed, 0.4f + 0.25f * draw, draw));
        // 활시위 "퉁" + 화살 "슉" (많이 당길수록 크고 묵직하게)
        Play("bowtwang", 0.55f + 0.35f * draw, 1.15f - 0.25f * draw);
        Play("arrowfly", 0.3f + 0.4f * draw, 1f + 0.4f * draw);
        BowFired = true;
    }

    // ================================================================= 연금술사: 시약 실험
    // 화염 → 빙결 → 산성 시약을 번갈아 채워 던지고, 가끔(15%) 불안정한 플라스크가 크게 터짐
    int reagent;
    static readonly Color[] ReagentColors = { new Color(1f, 0.55f, 0.25f), new Color(0.55f, 0.85f, 1f), new Color(0.55f, 1f, 0.35f) };
    public string NextReagentName => reagent switch { 0 => "화염 시약", 1 => "빙결 시약", _ => "산성 시약" };

    //   weakMul > 0: 떨어진 점이 빈틈 위면 그 보스 · 중간 보스만 배율 (평타 첫 병 · 장판 · 덤 폭발은 제외)
    void ThrowReagent(Vector3 from, Vector3 land, float dmg, int depth = 0, float weakMul = 0f)
    {
        int kind = reagent;
        reagent = (reagent + 1) % 3;
        // 현자의 돌이면 모두 불안정, 불안정 연구면 확률이 오름
        bool unstable = Special != null ? (Special.KitStoneActive || Random.value < Special.KitUnstableChance) : Random.value < 0.15f;
        if (depth == 0 && ForceUnstable()) unstable = true;
        // 불안정도 (1.0.2): 던질 때마다 오르고, 높을수록 크고 아프게 터짐
        float instR = InstRadiusMul, instD = InstDamageMul;
        if (depth == 0) AddInstability(unstable ? 12f : 5f);
        Color tint = unstable ? new Color(0.85f, 0.4f, 1f) : ReagentColors[kind];
        // 시약 농축: 한 바퀴 던질 때마다 다음 플라스크가 더 크고 아프게 (고유 스킬)
        float conc = depth == 0 ? SignatureSkills.FlaskThrow() : 1f;
        FlaskLob.Throw(from, land, 0.4f, (unstable ? 1.05f : 0.8f) * (conc > 1f ? 1.35f : 1f), tint, (p) =>
        {
            // 2.1.3: 너무 셌음 — 반지름 1.8 → 1.65, 피해 1.45 → 1.2배, 불안정 1.8 → 1.6배
            float r = 1.65f * blastMul * (unstable ? 1.6f : 1f) * FlaskRadiusMul * conc * instR;
            float hit = dmg * 1.2f * (unstable ? 1.6f : 1f) * FlaskDamageMul * conc * instD;
            FlaskExtras(p, r, hit, depth);
            Play("shatter", 0.55f, Random.Range(0.9f, 1.2f));
            if (unstable)
            {
                RunCounters.Both("unstable");          // 1.2~ 영혼 계약 · 보드
                // 불안정! 보라색 대폭발
                using var caughtHits = PhysHits.Circle(p, r);
                List<Collider2D> caught = caughtHits.List;
                DamageCircle(p, r, hit, 2.2f, weakMul);
                foreach (Collider2D c in caught)
                {
                    if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                    EnermyController e = c.GetComponent<EnermyController>();
                    if (e != null && e.IsDead) ChainPop(c.transform.position, hit);
                    else if (Special != null && Special.KitUnstableBurns) Burn.Apply(c.gameObject, hit * 0.3f, 3f);
                }
                Fx.Spawn("fx_alchemyblast", p, r * 2.4f, SkinFx.Tint(new Color(0.9f, 0.55f, 1f)), 16f, 0f, AttackLook.AreaOrder - 2);
                Fx.Spawn("fx_shock", p, r * 2.2f, new Color(0.85f, 0.5f, 1f, 0.8f), 20f);
                Hostile.Shake(0.18f);
                Play("boom", 0.7f, 1.1f);
                SignatureSkills.Flask(p, r, kind, true, new List<Collider2D>(caught), DeadCount(caught), conc > 1f);
                return;
            }
            using var insideHits = PhysHits.Circle(p, r);
            List<Collider2D> inside = insideHits.List;
            DamageCircle(p, r, hit, 1.2f, weakMul);
            Fx.Spawn("fx_alchemyblast", p, r * 2.2f, SkinFx.Tint(ReagentColors[kind]), 18f, 0f, AttackLook.AreaOrder - 2);      // 1.1.2: 적 · 맵 물체 · 적 이펙트 위 (크기 = 판정 그대로)
            foreach (Collider2D c in inside)
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && e.IsDead) { ChainPop(c.transform.position, hit); continue; }
                if (kind == 0) Burn.Apply(c.gameObject, hit * 0.3f, 2f);
                // 급속 냉동: 빙결 시약이 느리게 하는 대신 꽁꽁 얼림
                if (kind == 1 && e != null) e.Slow(card[1] > 0 ? 0f : 0.5f, card[1] > 0 ? 0.5f + 0.3f * card[1] : 1.5f);
                // 원소 융합: 불타는 적에게 빙결 시약이 닿으면 증기 폭발
                if (kind == 1 && card[5] > 0)
                {
                    Burn burn = c.GetComponent<Burn>();
                    if (burn != null)
                    {
                        Destroy(burn);
                        DamageCircle(c.transform.position, 1.8f, Damage * (0.8f + 0.7f * card[5]), 1.5f);
                        Fx.Spawn("fx_cloud", c.transform.position, 3.6f, new Color(1f, 1f, 1f, 0.8f), 16f);
                        Play("hiss", 0.5f, 1.4f);
                    }
                }
            }
            SignatureSkills.Flask(p, r, kind, false, new List<Collider2D>(inside), DeadCount(inside), conc > 1f);
            if (kind == 0) Play("ignite", 0.3f, 1.3f);
            if (kind == 1) Play("shimmer", 0.35f, 1.5f);
            if (kind == 2)
            {
                float acidTime = 2.5f + card[6];
                if (Special != null) Special.SpawnZone(p, r * 0.8f, acidTime, hit * 0.25f, new Color(0.45f, 1f, 0.3f, 0.6f));
                if (card[6] > 0) StartCoroutine(StickyAcid(p, r * 0.8f, acidTime));
                Play("fizz", 0.45f, 1f);
            }
        });
    }

    // 이 폭발로 쓰러진 적 수 (증폭 용액)
    static int DeadCount(List<Collider2D> cols)
    {
        int n = 0;
        foreach (Collider2D c in cols)
        {
            if (c == null) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) n++;
        }
        return n;
    }

    static Vector3 ClampRange(Vector3 from, Vector3 to, float range)
    {
        Vector3 d = to - from;
        return range > 0f && d.magnitude > range ? from + d.normalized * range : to;
    }

    // ================================================================= 우클릭 스킬
    public void UpdateUlt(SkillGauge gauge)
    {
        bool full = gauge != null && gauge.IsFull();
        // 2.1.8: 무기를 진화해도 모든 캐릭터의 우클릭은 원래 필살기 그대로 (회전 베기 · 출혈 돌진 · 화살비 · 대폭발)
        // 진화 무기의 궁극기는 필살기를 쓸 때 덧붙는 효과 (KitUltAdd, 세기 KitUltAddMul) → 원래 우클릭에 붙는 카드 · 고유 스킬이 모두 그대로 터짐
        // (예전엔 검사 · 도적 · 궁수는 우클릭이 진화 무기 궁극기로 바뀌어, 회전 베기 · 화살비에만 붙는 스킬 일부가 빠졌음)
        if (ultAiming && Time.timeScale == 0f) CancelUltAim();       // 멈춤
        switch (Id)
        {
            case CharacterId.Swordsman:
            case CharacterId.Alchemist:
                UpdateCharged(gauge, full);
                break;
            case CharacterId.Rogue:
                // 출혈 돌진: 누르고 있는 동안 돌진 경로를 보여 주고 떼면 돌진
                bool recast = Time.time < recastUntil;
                if (GameInput.UltDown && !Dashing && (full || recast)) ultAiming = true;
                if (!ultAiming) break;
                DrawUltAim(true, AddedUltShown && !recast);      // 1.0.1: 게이지 돌진이면 덧붙은 궁극기 범위도
                if (GameInput.UltHeld && !GameInput.UltUp) break;
                CancelUltAim();
                if (Dashing || !(full || Time.time < recastUntil)) break;
                {
                    recast = Time.time < recastUntil;
                    // 그림자 숙련 4단계: 게이지로 돌진한 뒤 잠깐은 한 번 더 공짜로 (게이지는 아껴 둠)
                    recastUntil = 0f;
                    Vector3 aimAt = Mouse;          // 조준 범위를 그린 자리 (돌진하면 카메라가 따라가 마우스의 월드 좌표가 바뀜)
                    StartCoroutine(BleedDash(((Vector2)(aimAt - transform.position)).normalized));
                    if (recast) break;
                    KitUltAdd(0.25f, aimAt);           // 게이지로 돌진할 때만 (공짜 연속 돌진에는 안 붙음) · 돌진 끝 무렵에 터지게
                    Spend(gauge);
                    if (RogueMastery >= 4) recastUntil = Time.time + 3f;
                }
                break;
            case CharacterId.Archer:
                UpdateRainAim(gauge, full);
                break;
            case CharacterId.Cartographer:
                UpdateFold(gauge, full);                    // 지도 접기
                break;
            case CharacterId.Clockmaker:
                UpdateRewind(gauge, full);                  // 되감기
                break;
            case CharacterId.Postman:
                UpdateExpress(gauge, full);                 // 속달
                break;
            case CharacterId.Ghost:
                UpdatePossess(gauge, full);                 // 빙의
                break;
        }
    }

    // 진화 무기의 궁극기를 원래 필살기에 덧붙임 (2.1.8~ 모든 캐릭터) · delay: 필살기가 닿을 무렵 터지게 (도적은 돌진 끝 무렵)
    void KitUltAdd(float delay, Vector3? aimAt = null)
    {
        if (Special == null || !Special.KitWeaponUltActive) return;
        if (delay <= 0f) { Special.KitWeaponUlt(KitUltAddMul, false, aimAt); return; }
        StartCoroutine(KitUltLater(delay, aimAt));
    }

    IEnumerator KitUltLater(float delay, Vector3? aimAt)
    {
        yield return new WaitForSeconds(delay);
        if (Special != null && Special.KitWeaponUltActive) Special.KitWeaponUlt(KitUltAddMul, false, aimAt);
    }

    // 우클릭 조준 (전용 무기 궁극기 · 도적 출혈 돌진): 누르고 있는 동안 범위 · 경로를 보여 주고 떼면 발동
    bool ultAiming;
    LineRenderer ultAimLine, ultAimRing;

    // 1.1.9~ 필살기 조준 · 발동 중인지 (「필살기 누르면 토글」이 헛누름을 풀 때 씀)
    public bool UltEngaged => ultAiming || charging || aiming || folding || rewinding || expressing || possessAiming;

    void CancelUltAim()
    {
        GameInput.ResetUltLatch();
        ultAiming = false;
        if (ultAimLine != null) ultAimLine.enabled = false;
        if (ultAimRing != null) ultAimRing.enabled = false;
    }

    // 무기 진화 뒤 덧붙는 궁극기가 있으면 그 범위도 조준 중에 보여 줌 (1.0.1)
    bool AddedUltShown => Special != null && Special.KitWeaponUltActive;

    // dash: 도적은 먼저 돌진하므로 돌진 경로와 도착 지점 기준 범위를 보여 줌 · shape: 전용 무기 궁극기 범위까지
    void DrawUltAim(bool dash, bool shape = true)
    {
        if (ultAimLine == null) ultAimLine = Hostile.NewLine("UltAimLine", Color.white, 0.12f, 25);
        if (ultAimRing == null) { ultAimRing = Hostile.NewLine("UltAimRing", Color.white, 0.12f, 25); ultAimRing.loop = true; }
        float pulse = 0.55f + 0.3f * Mathf.Sin(Time.time * 14f);
        Vector3 me = transform.position;
        Vector2 dir = ((Vector2)(Mouse - me)).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        Vector3 origin = me;
        ultAimLine.enabled = false;
        ultAimRing.enabled = false;

        if (dash)
        {
            origin = DashEnd(dir);
            Color purple = new Color(0.75f, 0.45f, 1f, pulse);
            ultAimLine.enabled = true;
            ultAimLine.positionCount = 2;
            ultAimLine.SetPosition(0, me);
            ultAimLine.SetPosition(1, origin);
            ultAimLine.startColor = ultAimLine.endColor = purple;
            ultAimLine.startWidth = ultAimLine.endWidth = 1.4f;
            if (!shape)
            {
                ultAimRing.enabled = true;
                Hostile.SetArc(ultAimRing, origin, 1f, 0f, 360f);
                ultAimRing.startColor = ultAimRing.endColor = purple;
                return;
            }
        }
        if (!shape || Special == null) return;

        Color c = SpecialAbilities.ColorOf(Special.UltId);
        c.a = pulse;
        switch (Special.KitUltAim(out float size, out Vector3 at))
        {
            case SpecialAbilities.UltAimShape.Point:
                ultAimRing.enabled = true;
                Hostile.SetArc(ultAimRing, at, size, 0f, 360f);
                break;
            case SpecialAbilities.UltAimShape.Around:
                ultAimRing.enabled = true;
                Hostile.SetArc(ultAimRing, origin, size, 0f, 360f);
                break;
            case SpecialAbilities.UltAimShape.Burst:
            {
                // 사방으로 뻗는 화살 열두 개 (천천히 돎): 범위가 아니라 날아가는 방향
                const int spokes = 12;
                ultAimLine.enabled = true;
                ultAimLine.positionCount = spokes * 4;
                float spin = Time.time * 30f;
                for (int i = 0; i < spokes; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, spin + i * 360f / spokes) * Vector2.right;
                    Vector2 side = new Vector2(-d.y, d.x);
                    Vector3 tip = origin + (Vector3)(d * size);
                    ultAimLine.SetPosition(i * 4, origin + (Vector3)(d * 1.2f));
                    ultAimLine.SetPosition(i * 4 + 1, tip);
                    ultAimLine.SetPosition(i * 4 + 2, tip - (Vector3)(d * 0.8f) + (Vector3)(side * 0.5f));     // 화살촉
                    ultAimLine.SetPosition(i * 4 + 3, origin + (Vector3)(d * 1.2f));
                }
                ultAimLine.startColor = ultAimLine.endColor = c;
                ultAimLine.startWidth = ultAimLine.endWidth = 0.18f;
                break;
            }
            case SpecialAbilities.UltAimShape.Line:
                if (dash) { ultAimRing.enabled = true; Hostile.SetArc(ultAimRing, origin, 1.5f, 0f, 360f); }
                else
                {
                    ultAimLine.enabled = true;
                    ultAimLine.positionCount = 2;
                    ultAimLine.SetPosition(0, origin);
                    ultAimLine.SetPosition(1, origin + (Vector3)(dir * size));
                    ultAimLine.startColor = ultAimLine.endColor = c;
                    ultAimLine.startWidth = ultAimLine.endWidth = 1.2f;
                }
                break;
        }
        ultAimRing.startColor = ultAimRing.endColor = c;
        ultAimRing.startWidth = ultAimRing.endWidth = 0.14f;
    }

    float lastUlt = -99f;

    void Spend(SkillGauge gauge)
    {
        lastUlt = Time.time;
        gauge.ResetSkillPoint();
        player.RaiseUltUsed();
    }

    // 무기 진화 뒤 원래 필살기에 덧붙는 진화 무기 궁극기 세기 (둘이 다 터지므로 궁극기는 절반)
    const float KitUltAddMul = 0.5f;

    void UpdateCharged(SkillGauge gauge, bool full)
    {
        if (!charging && GameInput.UltDown && full)
        {
            charging = true;
            charge = 0f;
            if (ring == null) ring = Hostile.NewLine("UltRing", Color.white, 0.14f, 25);
            ring.loop = true;
            ring.enabled = true;
            Play(Id == CharacterId.Alchemist ? "bubble" : "hum", 0.5f, 0.8f);
        }
        if (!charging) return;

        charge = Mathf.Min(1f, charge + Time.deltaTime / 1.5f);
        bool sword = Id == CharacterId.Swordsman;
        Vector3 at = sword ? transform.position : ClampRange(transform.position, Mouse, 14f);
        float radius = (sword ? 5f : (2f + 4f * charge)) * UltMul;
        Hostile.SetArc(ring, at, radius, 0f, 360f);
        Color c = sword ? new Color(0.55f, 0.75f, 1f) : new Color(0.55f, 1f, 0.45f);
        ring.startColor = ring.endColor = new Color(c.r, c.g, c.b, 0.35f + 0.5f * charge * (0.7f + 0.3f * Mathf.Sin(Time.time * 18f)));
        ring.startWidth = ring.endWidth = 0.1f + 0.15f * charge;
        if (sword && Random.value < 0.4f) Fx.Spawn("fx_sparkle", transform.position + (Vector3)(Random.insideUnitCircle * 1.5f), 0.6f, c, 18f);
        if (AddedUltShown) DrawUltAim(false, true);      // 1.0.1: 덧붙는 궁극기(예: 망치 대지 진동의 충격파 길)도 차징하는 동안 보여 줌

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        charging = false;
        ring.enabled = false;
        CancelUltAim();
        if (sword)
        {
            // 회전 베기: 누른 만큼 강해짐 (1.1.3: 3 + 5c → 2.5 + 4.5c, 밸런스 점검)
            float dmg = Damage * (2f + 3.5f * charge) * UltMul;      // 1.1.3 빌드 뒤 2.5 + 4.5c → 2 + 3.5c
            DamageCircle(transform.position, radius, dmg, 2.5f);
            PlayerMotion.Of(this).Spin(0.45f, 2f);      // 몸동작: 회전 베기 (1.0.1)
            SignatureSkills.Spin(transform.position, radius, dmg, charge);
            Fx.Spawn("fx_spinslash", transform.position, radius * 2.4f, Color.white, 22f);
            Fx.Spawn("fx_shock", transform.position, radius * 2.2f, new Color(0.6f, 0.8f, 1f, 0.8f), 20f);
            Play("slash", 1f, 0.8f);
            Play("spin", 0.9f, 1f);
            Play("whoosh", 0.6f, 0.6f);
            if (card[3] > 0) StartCoroutine(BladeStorm(1f + card[3], radius * 0.7f));
            KitUltAdd(0f);
        }
        else
        {
            if (Instability >= InstabilityMax - 0.001f) RunCounters.Add("alch.fullpour");       // 1.2~ 영혼 계약 「가득 채워 쏟기」
            float dmg = Damage * (4f + 8f * charge) * UltMul * SignatureSkills.BigFlaskMul() * ConsumeInstability();     // 증폭 용액 · 불안정도를 모두 쏟음
            PlayerMotion.Of(this).Throw(0.35f, (Vector2)(at - transform.position));     // 몸동작: 플라스크 던지기
            FlaskLob.Throw(player.MuzzlePosition, at, 0.55f, 1.6f, new Color(0.8f, 1f, 0.7f), (p) =>
            {
                DamageCircle(p, radius, dmg, 3f);
                SignatureSkills.BigFlask(p, radius);
                Fx.Spawn("fx_alchemyblast", p, radius * 2.4f, Color.white, 16f);
                if (Special != null) Special.SpawnZone(p, radius * 0.7f, 4f, Damage * 0.5f, new Color(0.45f, 1f, 0.35f, 0.7f));
                Play("boom", 1f, 0.9f);
                Play("shatter", 0.8f, 0.8f);
                Play("fizz", 0.7f, 0.8f);
                if (card[3] > 0) Shards(p, radius, 2 + 2 * card[3]);
            });
            KitUltAdd(0f);          // 무기 진화 뒤: 진화 무기의 궁극기가 절반 세기로 덧붙음
        }
        Spend(gauge);
    }

    // 출혈 돌진이 멈출 자리 (벽 · 경기장 끝 앞). 조준 미리보기와 실제 돌진이 같은 값을 씀
    Vector3 DashEnd(Vector2 dir)
    {
        float dist = RogueMastery >= 3 ? 32f : 25f;          // 그림자 숙련 3단계: 더 멀리 · 더 오래 무적
        Vector3 from = transform.position;
        float len = dist;
        for (float d = 0.5f; d <= dist; d += 0.5f)
        {
            Vector3 p = from + (Vector3)(dir * d);
            if (Hostile.IsWall(p) || (Hostile.ClampArena(p) - p).sqrMagnitude > 0.01f) { len = d - 0.5f; break; }
        }
        return from + (Vector3)(dir * len);
    }

    // 도적 출혈 돌진: 무적 상태로 마우스 쪽으로 빠르게 돌진, 지나간 적마다 한 번 베고 출혈
    public IEnumerator BleedDash(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.01f) dir = body.flipX ? Vector2.left : Vector2.right;
        int mastery = RogueMastery;
        const float time = 0.32f, width = 1.4f;
        Vector3 from = transform.position;
        Vector3 to = DashEnd(dir);

        dashUntil = Time.time + time;
        SignatureSkills.DashStart(from);                         // 연막
        // 돌진하는 동안 + 끝난 뒤 0.4초 무적
        player.GrantInvincibility(time + (mastery >= 3 ? 0.8f : 0.4f));
        if (card[2] > 0) StartCoroutine(ShadowClone(from, 1f + card[2]));
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        PlayerMotion.Of(this).Lunge(time + 0.1f, dir);      // 몸동작: 출혈 돌진
        Fx.Spawn("fx_stealth", from, 3f, Color.white, 16f);
        Play("whoosh", 1f, 1.4f);
        Play("slash", 0.7f, 1.5f);

        HashSet<Collider2D> hit = new HashSet<Collider2D>();
        int ghosts = 0;
        Vector3 prev = from;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            Vector3 p = Vector3.Lerp(from, to, t / time);
            transform.position = p;
            Cut(prev, p, width, dir, hit);
            prev = p;
            // 보라 잔상과 그림자 줄기
            if (t >= ghosts * 0.03f)
            {
                ghosts++;
                GameObject g = SpecialAbilities.MakeSprite("DashGhost", body.sprite, p, 1f, new Color(0.6f, 0.35f, 0.9f, 0.55f), "Character", -1);
                g.transform.localScale = transform.lossyScale;
                g.GetComponent<SpriteRenderer>().flipX = body.flipX;
                g.AddComponent<FadeOut>().duration = 0.3f;
                Fx.Spawn("fx_shadowdash", p - (Vector3)(dir * 1.2f), 1.2f, Color.white, 24f, rot, 13);
            }
            yield return null;
        }
        transform.position = to;
        Cut(prev, to, width, dir, hit);
        Fx.Spawn("fx_stealth", to, 2.4f, new Color(1f, 0.7f, 0.8f), 18f);
        if (hit.Count > 0) Hostile.Shake(0.1f);
        if (card[3] > 0) StarBurst(to, 4 + 4 * card[3]);
        RunCounters.Max("dash.hits", hit.Count);       // 1.2~ 영혼 계약 「한 번의 돌진, 여덟」
        DashMastery(to, mastery, hit.Count);
        SignatureSkills.DashEnd(from, to, hit);                  // 급습 · 궤적 칼날 · 그림자 매듭
    }

    // ================================================================= 도적 그림자 숙련
    // 영혼 트리 필살기 가지에서 한 단계씩 배우면 출혈 돌진에 유틸이 붙음 (SpecialAbilities.Evolution 의 u.shadow1~5)
    //   1 이동 속도 · 2 착지 둔화 · 3 거리 · 무적 · 4 연속 돌진 · 5 게이지 반환
    public static readonly string[] MasteryNames =
    {
        "돌진 뒤 2초 동안 이동 속도 +35%",
        "돌진이 끝난 자리 주변의 적을 1.5초 동안 60% 느리게",
        "돌진 거리 +30% · 돌진 뒤 무적 시간 두 배",
        "돌진 뒤 3초 안에 우클릭으로 한 번 더 돌진 (게이지 없이)",
        "돌진으로 벤 적 하나당 필살기 게이지 4% 되돌려 받음 (최대 40%)",
    };
    float recastUntil;
    [HideInInspector] public int shadowLevel;        // 배운 그림자 숙련 단계 (0 = 없음)

    int RogueMastery => Id == CharacterId.Rogue ? shadowLevel : 0;

    void DashMastery(Vector3 at, int mastery, int hits)
    {
        if (mastery >= 1) StartCoroutine(DashHaste(2f, 1.35f));
        if (mastery >= 2)
        {
            using (var ovl = PhysHits.Circle(at, 4f)) foreach (Collider2D c in ovl)
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && !e.IsDead) e.Slow(0.4f, 1.5f);
            }
            Fx.Spawn("fx_shock", at, 8f, new Color(0.6f, 0.4f, 0.9f, 0.6f), 20f);
        }
        if (mastery >= 4 && recastUntil > Time.time)
            Fx.Spawn("fx_sparkle", at, 1.6f, new Color(0.8f, 0.6f, 1f), 20f);
        if (mastery >= 5 && hits > 0)
        {
            if (gaugeRef == null) gaugeRef = FindFirstObjectByType<SkillGauge>();
            if (gaugeRef != null) gaugeRef.AddSkillPoint(gaugeRef.MaxSkillPoint * Mathf.Min(0.4f, 0.04f * hits));
        }
    }

    // 1.0.1: 연속 돌진 때 곱으로 겹치지 않게 (이미 빨라져 있으면 시간만 늘림)
    float hasteUntil;
    bool hasted;
    IEnumerator DashHaste(float seconds, float mul)
    {
        hasteUntil = Time.time + seconds;
        if (hasted) yield break;
        hasted = true;
        player.speed *= mul;
        while (Time.time < hasteUntil) yield return null;
        if (player != null) player.speed /= mul;
        hasted = false;
    }

    // 이번 프레임에 지나간 구간 전체 (프레임이 길어도 건너뛰지 않게)
    void Cut(Vector3 a, Vector3 b, float width, Vector2 dir, HashSet<Collider2D> hit)
    {
        using (var ovl = PhysHits.Circle((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + width)) foreach (Collider2D c in ovl)
        {
            if (!c.CompareTag("enermy") && !c.CompareTag("boss")) continue;
            if (Hostile.DistanceToSegment(c.transform.position, a, b) > width) continue;
            if (!hit.Add(c)) continue;
            Specials.Damage(c.gameObject, Damage * 2.2f * UltMul, dir, 1f);
            Bleed.Apply(c.gameObject, Damage * 1.5f * UltMul, 4f);
            Fx.Spawn("fx_bleed", c.transform.position, 1.4f, Color.white, 16f);
            Play("crack", 0.35f, 1.4f);
        }
    }

    const int RainBossHits = 6;

    // 화살비: 마우스 둘레에 화살이 1.2초 동안 쏟아짐
    IEnumerator ArrowRain(Vector3 center)
    {
        Play("bowtwang", 0.8f, 0.85f);
        PlayerMotion.Of(this).Recoil(Vector2.up + (Vector2)(center - transform.position).normalized * 0.3f, 1.5f);     // 몸동작: 하늘로 쏘며 뒤로 젖혀짐 (1.0.1)
        float radius = 4f * UltMul;
        Hostile.Circle(center, radius, 0.4f, new Color(0.6f, 1f, 0.5f, 0.6f));
        SignatureSkills.Rain(center, radius);
        Play("whoosh", 0.8f, 0.9f);
        // 몸집이 큰 보스는 화살이 거의 다 맞아 피해가 너무 컸음: 한 번의 화살비에 보스 하나당 6발까지만
        Dictionary<Collider2D, int> bossHits = new Dictionary<Collider2D, int>();
        // 2.1.8: 필살기 강화(UltMul)가 화살 수 대신 한 발 피해에 붙음 (보스는 6발 제한이라 화살 수가 늘어도 보스 피해는 그대로였음)
        for (int i = 0; i < 20; i++)
        {
            Vector3 at = center + (Vector3)(Random.insideUnitCircle * radius);
            Fx.Spawn("fx_arrowrain", at + Vector3.up * 1f, 2f, Color.white, 18f);
            using (var ovl = PhysHits.Circle(at, 1.3f)) foreach (Collider2D c in ovl)
            {
                bool boss = c.CompareTag("boss");
                if (!boss && !c.CompareTag("enermy")) continue;
                if (boss)
                {
                    bossHits.TryGetValue(c, out int n);
                    if (n >= RainBossHits) continue;
                    bossHits[c] = n + 1;
                }
                Specials.Damage(c.gameObject, Damage * 2.5f * UltMul, (c.transform.position - at).normalized, 0.5f);
            }
            if (i % 4 == 0) Play("arrowfly", 0.35f, Random.Range(0.8f, 1.2f));
            if (i % 3 == 0) Play("pluck", 0.3f, Random.Range(0.85f, 1.25f));
            yield return new WaitForSeconds(0.06f);
        }
        if (card[3] > 0) StartCoroutine(Thorns(center, radius, 2f + card[3]));
    }

    // 궁수 우클릭: 누르고 있는 동안 화살비가 떨어질 자리(원)가 마우스를 따라다니고, 떼면 쏟아짐
    bool aiming;
    LineRenderer aimRing;

    void UpdateRainAim(SkillGauge gauge, bool full)
    {
        if (!aiming && GameInput.UltDown && full)
        {
            aiming = true;
            Play("bowdraw", 0.5f, 0.8f);
        }
        if (!aiming) return;

        Vector3 at = Mouse;
        if (aimRing == null) aimRing = Hostile.NewLine("RainAim", Color.white, 0.12f, 25);
        aimRing.loop = true;
        aimRing.enabled = true;
        Hostile.SetArc(aimRing, at, 4f * UltMul, 0f, 360f);
        aimRing.startColor = aimRing.endColor = new Color(0.6f, 1f, 0.5f, 0.5f + 0.3f * Mathf.Sin(Time.time * 14f));
        if (AddedUltShown) DrawUltAim(false, true);      // 1.0.1: 덧붙는 궁극기 범위도

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        aiming = false;
        aimRing.enabled = false;
        CancelUltAim();
        StartCoroutine(ArrowRain(at));
        KitUltAdd(0f);
        Spend(gauge);
    }

    // ================================================================= 캐릭터 전용 레벨업 카드 (특수 능력)
    float stillTime;
    int comboCount;

    // 검사 굳건한 자세: 우클릭을 모으는 동안 받는 피해 감소 (PlayerController.TryHit)
    // 검사: 휘두르는 동안(근접 특성) 받는 피해 절반, 굳건한 자세 카드는 우클릭을 모으는 동안 받는 피해 감소
    float swingGuardUntil;
    public float TakenMul
    {
        get
        {
            if (Id != CharacterId.Swordsman) return 1f;
            float mul = 1f;
            if (Time.time < swingGuardUntil) mul *= 0.5f;
            if (Charging && card[5] > 0) mul *= 1f - (0.15f + 0.15f * card[5]);
            return mul;
        }
    }

    // 적이 받는 피해 배율 (SpecialAbilities.KitDamageHook): 도적 급소 노리기 · 궁수 사냥감 표식
    public float TargetDamageMul(EnermyController e)
    {
        if (Id == CharacterId.Rogue && card[5] > 0 && e.GetComponent<Bleed>() != null) return 1f + 0.3f * card[5];
        if (Id == CharacterId.Archer && card[6] > 0 && HuntMark.Has(e.gameObject)) return 1.05f + 0.1f * card[6];
        return 1f;
    }

    // 도적 표창 회수: 탄창이 비면 확률로 재장전 없이 절반을 되찾음
    public void OnMagEmpty()
    {
        if (Id != CharacterId.Rogue || card[6] <= 0 || Random.value >= 0.25f * card[6]) return;
        player.NowBullet = Mathf.Max(1, player.MaxBullet / 2);
        Fx.Spawn("fx_sparkle", transform.position, 1.2f, new Color(0.85f, 0.7f, 1f), 20f);
        Play("clank", 0.4f, 1.8f);
    }

    // 연금술사 끈적한 산성: 웅덩이 위의 적을 느리게
    IEnumerator StickyAcid(Vector3 p, float r, float duration)
    {
        for (float t = 0f; t < duration; t += 0.3f)
        {
            using (var ovl = PhysHits.Circle(p, r)) foreach (Collider2D c in ovl)
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && !e.IsDead) e.Slow(0.6f, 0.4f);
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // 도적 사냥의 기세: 처치할 때마다 스킬 게이지 (레벨마다 최대치의 1%, 지금 공격력이 처음보다 높을수록 더 · 최대 4배)
    float baseDamage;
    SkillGauge gaugeRef;

    void OnKill(Vector3 pos)
    {
        GhostKill(pos);         // 유령: 영혼 · 원혼 · 얼음 파편 (CharacterKit.Ghost)
        if (card[4] <= 0 || player == null || Time.time - lastUlt < 1f) return;     // 필살기 직후 1초는 쉼
        if (gaugeRef == null) gaugeRef = FindFirstObjectByType<SkillGauge>();
        if (gaugeRef == null || gaugeRef.IsFull()) return;
        float power = Mathf.Clamp(Damage / Mathf.Max(0.01f, baseDamage), 1f, 4f);
        gaugeRef.AddSkillPoint(gaugeRef.MaxSkillPoint * 0.01f * card[4] * power);
    }

    public void CardPicked(int slot)
    {
        if (Id == CharacterId.Alchemist && slot == 2 && homunculus == null) StartCoroutine(Homunculus());
    }

    // 검사: 검기 · 흡혈 베기 · 쳐내기 (평타를 휘두를 때마다)
    void SwingCards(Vector3 origin, Vector2 dir, float reach, float half, int hits)
    {
        if (card[0] > 0)
        {
            // 검기: 휘두른 방향으로 적을 꿰뚫는 칼바람이 날아감 (1.1.2~ 판정 세로 1.7 → 3.4~4.4 로 커져 피해 40 · 60 · 80% → 30 · 45 · 60%)
            Projectile(origin + (Vector3)(dir * 1f), dir, Damage * (0.15f + 0.15f * card[0]), 9999, 22f, 14f, "fx_swordwave", 3.4f, new Color(0.7f, 0.9f, 1f, 0.9f), false);
        }
        // 근접 특성: 휘두르는 동안 받는 피해 절반
        // 1.1.3: 0.3 → 0.2초 (후반 베기 간격이 0.5초 아래로 내려가면 절반 보호가 거의 늘 켜져 있었음 · 검사가 사망률 가장 낮은 영웅이라 근접 위험 보정이 겹침)
        swingGuardUntil = Time.time + 0.2f;
        // 근접 특성: 벤 적(최대 3) 하나당 체력 0.2 회복 (1.1.3: 0.3 → 0.2), 흡혈 베기 카드가 레벨마다 +0.5
        if (hits > 0 && player.PlayerHealth > 0f)
        {
            float heal = Mathf.Min(hits, 3) * (0.1f + 0.35f * card[1]);     // 흡혈 베기: 0.45 / 0.8 / 1.15 (1.1.3 빌드 뒤 0.2 + 0.5c → 0.1 + 0.35c)
            player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + heal);
            if (card[1] > 0) Fx.Spawn("fx_sparkle", transform.position, 1.2f, new Color(1f, 0.4f, 0.45f), 20f);
        }
        if (card[2] > 0)
        {
            // 쳐내기: 휘두르는 범위 안의 적 투사체를 베어 없앰
            int parried = 0;
            for (int i = HostileProjectile.Live.Count - 1; i >= 0; i--)
            {
                HostileProjectile hp = HostileProjectile.Live[i];
                if (hp == null) continue;
                Vector2 to = hp.transform.position - origin;
                if (to.magnitude > reach + 0.5f || (to.sqrMagnitude > 0.25f && Vector2.Angle(dir, to) > half + 10f)) continue;
                Fx.Spawn("fx_spark", hp.transform.position, 1.2f, new Color(1f, 0.95f, 0.6f), 24f);
                Destroy(hp.gameObject);
                parried++;
            }
            if (parried > 0) { Play("ding", 0.5f, 1.8f); SignatureSkills.Parried(); }
        }
    }

    // 검사 칼바람: 회전 베기 뒤 몇 초 동안 칼바람이 몸을 감싸고 돌며 주변을 벰
    IEnumerator BladeStorm(float duration, float radius)
    {
        FxAnim storm = Fx.Play("fx_tornado", transform.position, radius * 2f, new Color(0.7f, 0.85f, 1f, 0.55f), 16f, 0f, 13, true, duration);
        float tick = 0f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (storm != null) storm.transform.position = transform.position;
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.3f;
                DamageCircle(transform.position, radius, Damage * 0.35f, 0.6f);
                Play("whoosh", 0.25f, 1.6f);
            }
            yield return null;
        }
    }

    // 도적 표창: 갈고리 표창(출혈) · 도탄 표창(가까운 다른 적에게 튕김)
    Bullet Shuriken(Vector3 start, Vector2 dir, float dmg, int bounces)
    {
        Bullet b = Projectile(start, dir, dmg, player.pene, 45f, 0f, "fx_shuriken", 1.1f, Color.white, true);
        if (b == null) return null;
        if (card[1] > 0) b.onHitEnemy += (s, c) => Bleed.Apply(c.gameObject, Damage * 0.2f * card[1], 3f);
        if (bounces > 0)
        {
            bool bounced = false;
            b.onHitEnemy += (s, c) =>
            {
                if (bounced || c == null) return;
                Transform next = NearestOther(c.transform.position, 9f, c.transform, s.Direction, 360f);
                if (next == null) return;
                bounced = true;
                Bullet r = Shuriken(c.transform.position, (next.position - c.transform.position).normalized, s.damage * 0.8f, bounces - 1);
                EnermyController e = c.GetComponent<EnermyController>();
                if (r != null && e != null) r.hitOnce = new HashSet<int> { e.GetInstanceID() };
            };
        }
        return b;
    }

    // 회전 표창 「귀환하는 칼날」: 돌아오는 표창이 둘로 갈라질 때 — 평타 표창과 같은 그림 · 판정 · 색
    public Bullet TwinShuriken(Vector3 at, Vector2 dir, Bullet like)
    {
        if (like == null) return null;
        Bullet b = Projectile(at, dir, like.damage, 2, like.speed, 0f, "fx_shuriken", 1.1f, Color.white, true);
        if (b != null && like.TryGetComponent(out SpriteRenderer from) && b.TryGetComponent(out SpriteRenderer to)) to.color = from.color;
        return b;
    }

    // 도적 그림자 분신: 돌진을 시작한 자리에 분신이 남아 가까운 적에게 표창을 던짐
    IEnumerator ShadowClone(Vector3 at, float duration)
    {
        GameObject g = SpecialAbilities.MakeSprite("ShadowClone", body.sprite, at, 1f, new Color(0.55f, 0.3f, 0.85f, 0.7f), "Character", -1);
        g.transform.localScale = transform.lossyScale;
        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        sr.flipX = body.flipX;
        Fx.Spawn("fx_stealth", at, 2f, new Color(0.8f, 0.6f, 1f), 18f);
        float next = 0.1f;
        for (float t = 0f; t < duration && g != null; t += Time.deltaTime)
        {
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                Transform e = Specials.NearestEnemy(at, 14f);
                next = e != null ? 0.35f : 0.1f;
                if (e != null)
                {
                    Vector2 d = ((Vector2)(e.position - at)).normalized;
                    sr.flipX = d.x < 0f;
                    // 2.2.1: 분신 표창도 내 표창과 같게 (갈고리 표창 출혈 · 도탄 표창 · 진화 형태의 독 · 도박 카드 · 와이어)
                    ThrowOne(at, d, Damage * 0.7f, 0.75f);
                    Play("whoosh", 0.2f, 2f);
                }
            }
            yield return null;
        }
        if (g != null) g.AddComponent<FadeOut>().duration = 0.3f;
    }

    // 도적 표창 폭풍: 돌진이 끝난 자리에서 표창이 사방으로 퍼짐
    void StarBurst(Vector3 at, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (360f / count * i) * Mathf.Deg2Rad;
            Shuriken(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), Damage * 0.8f, 0);
        }
        Play("whoosh", 0.6f, 1.9f);
    }

    // 가장 가까운 다른 적 (except 제외, ahead 방향에서 maxAngle 안쪽만)
    static Transform NearestOther(Vector3 from, float range, Transform except, Vector2 ahead, float maxAngle)
    {
        Transform best = null;
        float bestDist = range;
        using (var ovl = PhysHits.Circle(from, range)) foreach (Collider2D c in ovl)
        {
            if (c.transform == except || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            EnermyController e = c.GetComponent<EnermyController>();
            if (e != null && e.IsDead) continue;
            Vector2 to = c.transform.position - from;
            if (maxAngle < 180f && Vector2.Angle(ahead, to) > maxAngle) continue;
            if (to.magnitude < bestDist) { bestDist = to.magnitude; best = c.transform; }
        }
        return best;
    }

    // 궁수 분열 화살: 가득 당긴 화살이 처음 맞힌 적에게서 작은 화살 여러 갈래로 갈라짐
    void SplitArrow(Bullet arrow, Collider2D c)
    {
        if (c == null) return;
        int n = 1 + card[0];
        EnermyController e = c.GetComponent<EnermyController>();
        foreach (Vector2 d in Spread(arrow.Direction, n, 25f))
        {
            Bullet b = Projectile(c.transform.position, d, arrow.damage * 0.4f, 1, 50f, 10f, "fx_arrow", 0.35f, new Color(0.8f, 1f, 0.7f), false);
            SignatureSkills.Arrow(b, 1f, true);
            if (b != null && e != null) b.hitOnce = new HashSet<int> { e.GetInstanceID() };
        }
    }

    // 궁수 메아리 화살: 쏜 화살을 0.25초 뒤 푸른 유령 화살이 따라 쏨 (그때의 활 위치에서)
    // 2.2.1: 유령 화살도 진화 형태(그물 · 투창 · 연사)와 고유 스킬 화살 효과를 받음 (예전엔 맨 화살)
    IEnumerator EchoArrow(Vector2 dir, float dmg, float speed, float size, float draw)
    {
        yield return new WaitForSeconds(0.25f);
        Vector3 start = player.MuzzlePosition;
        Bullet b = Projectile(start, dir, dmg, player.pene + 1, speed, 0f, "fx_arrow", size, new Color(0.55f, 0.85f, 1f, 0.7f), false);
        if (b == null) yield break;
        ArrowForm(b, draw, start);
        SignatureSkills.Arrow(b, draw);         // 원래 화살과 같은 효과 (집중 호흡 · 꿰뚫는 시선 · 가시 씨앗 · 울림 · 표식 전염)
        Play("arrowfly", 0.25f, 1.6f);
    }

    // 궁수 강풍 화살: 가득 당긴 화살에 맞은 적이 크게 밀려나고 잠깐 기절 (0.4 · 0.6 · 0.8초)
    void GustArrow(Bullet b)
    {
        b.onHitEnemy += (arrow, c) =>
        {
            if (c == null) return;
            Specials.Damage(c.gameObject, arrow.damage * 0.1f, arrow.Direction, 3.5f);
            if (c.TryGetComponent(out EnermyController e) && !e.IsDead) e.Slow(0f, 0.2f + 0.2f * card[2]);
            Fx.Spawn("fx_shock", c.transform.position, 2.4f, new Color(0.8f, 1f, 0.9f, 0.7f), 22f);
        };
    }
    // 궁수 가시 덤불: 화살비가 떨어진 자리에 가시 덤불이 남아 적을 느리게 하고 찌름
    IEnumerator Thorns(Vector3 center, float radius, float duration)
    {
        if (Special != null) Special.SpawnZone(center, radius, duration, Damage * 0.4f, new Color(0.45f, 0.7f, 0.25f, 0.6f));
        for (float t = 0f; t < duration; t += 0.25f)
        {
            using (var ovl = PhysHits.Circle(center, radius)) foreach (Collider2D c in ovl)
            {
                EnermyController e = c.GetComponent<EnermyController>();
                if (e != null && !e.IsDead) e.Slow(0.45f, 0.4f);
            }
            Fx.Spawn("fx_spike", center + (Vector3)(Random.insideUnitCircle * radius), 1.2f, new Color(0.6f, 0.9f, 0.4f), 18f);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // 연금술사 연쇄 반응: 폭발로 쓰러진 적이 그 자리에서 한 번 더 터짐 (연쇄 폭발은 다시 이어지지 않음)
    void ChainPop(Vector3 pos, float hit)
    {
        if (card[0] <= 0) return;
        DamageCircle(pos, 1.5f, hit * (0.2f + 0.2f * card[0]), 1f);
        Fx.Spawn("fx_alchemyblast", pos, 3.4f, new Color(1f, 0.8f, 0.4f), 20f);
        Play("shatter", 0.3f, 1.4f);
        SignatureSkills.ChainPopped(pos, hit);
    }

    // 연금술사 파편 플라스크: 대폭발이 작은 플라스크 여러 개로 흩어져 다시 터짐
    void Shards(Vector3 p, float radius, int count)
    {
        float dmg = Damage * 1.5f;
        for (int i = 0; i < count; i++)
        {
            float a = (360f / count * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector3 land = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(radius * 0.6f, radius * 1.1f);
            FlaskLob.Throw(p, land, 0.35f, 0.6f, new Color(0.7f, 1f, 0.5f), (q) =>
            {
                DamageCircle(q, 1.5f, dmg, 1f);
                Fx.Spawn("fx_alchemyblast", q, 3.4f, new Color(0.6f, 1f, 0.45f), 18f);
                Play("shatter", 0.3f, 1.3f);
            });
        }
    }

    // 연금술사 호문쿨루스: 머리 위를 맴도는 작은 조수가 가까운 적에게 작은 플라스크를 던짐
    GameObject homunculus;

    IEnumerator Homunculus()
    {
        Sprite[] f = Fx.Frames("fx_flask");
        if (f.Length == 0) yield break;
        homunculus = SpecialAbilities.MakeSprite("Homunculus", f[0], transform.position, 1.25f / f[0].bounds.size.y, new Color(0.6f, 1f, 0.5f), "Character", 2);      // 0.7 은 너무 작아 잘 안 보였음
        float cd = 1f, t = 0f;
        while (homunculus != null)
        {
            t += Time.deltaTime;
            Vector3 home = transform.position + new Vector3(Mathf.Cos(t * 2f) * 1.4f, 1.3f + Mathf.Sin(t * 4f) * 0.2f, 0f);
            homunculus.transform.position = Vector3.Lerp(homunculus.transform.position, home, 8f * Time.deltaTime);
            cd -= Time.deltaTime;
            if (cd <= 0f)
            {
                Transform e = Specials.NearestEnemy(homunculus.transform.position, 12f);
                cd = e != null ? 3.4f - 0.7f * card[2] : 0.3f;
                if (e != null)
                {
                    float dmg = Damage * 1.4f * SignatureSkills.HomunculusMul;      // 조수 개조
                    FlaskLob.Throw(homunculus.transform.position, e.position, 0.45f, 0.55f, new Color(0.6f, 1f, 0.5f), (q) =>
                    {
                        DamageCircle(q, 1.4f, dmg, 1f);
                        SignatureSkills.HomunculusLand(q, dmg);
                        Fx.Spawn("fx_alchemyblast", q, 3f, new Color(0.6f, 1f, 0.45f), 18f);
                        Play("shatter", 0.3f, 1.4f);
                    });
                }
            }
            yield return null;
        }
    }
}

// 궁수 사냥감 표식: 표식이 붙은 적은 피해를 더 받음 (머리 위 과녁)
public class HuntMark : MonoBehaviour
{
    float until;
    FxAnim icon;

    public static bool Has(GameObject go) => go != null && go.TryGetComponent(out HuntMark m) && Time.time < m.until;

    public static void Apply(GameObject target, float seconds)
    {
        if (target == null) return;
        HuntMark m = target.GetComponent<HuntMark>();
        if (m == null) m = target.AddComponent<HuntMark>();
        m.until = Time.time + seconds;
    }

    void Update()
    {
        if (Time.time > until)
        {
            if (icon != null) Destroy(icon.gameObject);
            Destroy(this);
            return;
        }
        if (icon == null) icon = Fx.Play("fx_reticle", transform.position, 1.2f, new Color(1f, 0.4f, 0.35f, 0.9f), 8f, 0f, 19, true, 99f);
        if (icon != null) icon.transform.position = transform.position + Vector3.up * 1.2f;
    }

    void OnDestroy()
    {
        if (icon != null) Destroy(icon.gameObject);
    }
}

// 출혈: 몇 초 동안 0.25초마다 피해, 핏방울이 떨어짐 (다시 걸리면 시간 갱신)
public class Bleed : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    public float dps;
    public float until;
    float tick, drip;
    SpriteRenderer body;
    bool bodyChecked;

    public static void Apply(GameObject target, float dps, float duration)
    {
        if (target == null) return;
        Bleed b = target.GetComponent<Bleed>();
        if (b == null) b = target.AddComponent<Bleed>();
        b.dps = Mathf.Max(b.dps, dps);
        b.until = Time.time + duration;
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        if (Time.time > until) { Destroy(this); return; }
        drip += Time.deltaTime;
        if (drip >= 0.25f)
        {
            drip = 0f;
            // 화면 밖 적은 핏방울을 만들지 않음 (보이지 않으니 겉모습 같음 · 피해는 그대로)
            if (!bodyChecked) { bodyChecked = true; TryGetComponent(out body); }
            if (body == null || body.isVisible)
                Fx.Spawn("fx_bleed", transform.position + (Vector3)(Random.insideUnitCircle * 0.4f), 1.4f, Color.white, 14f);
        }
        tick += Time.deltaTime;
        if (tick >= 0.25f)
        {
            tick = 0f;
            SignatureSkills.DotTick = true;        // 지속 피해 (맞힐 때 효과가 붙지 않게)
            Specials.Damage(gameObject, dps * 0.25f, Vector3.zero, 0f);
            SignatureSkills.DotTick = false;
        }
    }
}

// 투사체 · 이펙트 도트를 돌려 가며 보여줌 (필요하면 빙글빙글)
public class FrameLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 14f;
    public float spin;
    SpriteRenderer sr;
    float t;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        t += Time.deltaTime;
        sr.sprite = frames[(int)(t * fps) % frames.Length];
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
    }
}

// 포물선으로 날아가 떨어진 자리에서 터지는 플라스크
public class FlaskLob : MonoBehaviour
{
    readonly string dmgSource = DamageSource.Current;     // 만들어질 때의 피해 출처 (RunStats)
    Vector3 from, to;
    float time, t;
    System.Action<Vector3> onLand;

    public static void Throw(Vector3 from, Vector3 to, float time, float size, Color tint, System.Action<Vector3> onLand)
    {
        // 1.1.2: 새 플라스크 그림(시약색 병 · 흰 반짝임 · 어두운 외곽선) + 빛 무리 · 잔상, 보이는 크기 2.4 → 2.7 → 3배 (터지는 자리 판정은 그대로)
        Sprite[] f = Fx.Frames("atk_flask");
        bool look = f.Length > 0;
        if (!look) f = Fx.Frames("fx_flask");
        size *= 1.5f;           // 날아가는 플라스크가 너무 작아 잘 안 보였음 (모든 플라스크 공통)
        if (look) size *= AttackLook.BigMul(AttackLook.Tier);
        GameObject go = new GameObject("Flask");
        go.transform.position = from;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = AttackLook.Order;
        sr.color = tint;
        if (f.Length > 0)
        {
            sr.sprite = f[0];
            go.transform.localScale = Vector3.one * (size / f[0].bounds.size.y);
            FrameLoop loop = go.AddComponent<FrameLoop>();
            loop.frames = f;
            loop.spin = -600f;
            if (look)
            {
                BulletGlow glow = go.AddComponent<BulletGlow>();
                glow.styled = true;
                glow.frames = f;
                glow.coreFrames = Fx.Frames("atk_flask_core");
                glow.haloMul = 1.3f;
            }
        }
        FlaskLob l = go.AddComponent<FlaskLob>();
        l.from = from;
        l.to = to;
        l.time = time;
        l.onLand = onLand;
    }

    void Update()
    {
        using var source = DamageSource.As(dmgSource);
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / time);
        Vector3 p = Vector3.Lerp(from, to, k);
        p.y += Mathf.Sin(k * Mathf.PI) * 2f;
        transform.position = p;
        if (k < 1f) return;
        onLand?.Invoke(to);
        Destroy(gameObject);
    }
}
