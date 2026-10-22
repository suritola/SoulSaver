using UnityEngine;

// 플레이어 모습: 들고 있는 무기에 따라 손에 드는 무기 도트가 바뀌고(Resources/Weapons),
// 쏠 때마다 무기마다 다른 발사 연출 (반동 · 불티 · 탄피 · 연기 등)
// 거너는 총을 지운 캐릭터 그림(Resources/Characters/gunner_nogun)을 쓰고, 기본 권총도 따로 들어 마우스를 따라 겨눔
public class PlayerLook : MonoBehaviour
{
    public static PlayerLook Instance { get; private set; }

    PlayerController player;
    SpriteRenderer body;
    SpriteRenderer held;
    int shownWeapon = -99;
    // 거너: 애니메이션 프레임 이름 → 총을 지운 같은 프레임
    readonly System.Collections.Generic.Dictionary<string, Sprite> noGun = new System.Collections.Generic.Dictionary<string, Sprite>();
    Vector2 aim = Vector2.right;
    float kick, kickAngle;        // 반동: 뒤로 밀린 거리 · 들린 각도 (점점 돌아옴)
    // 검사 평타: 검을 위→아래(또는 아래→위)로 크게 휘두른 뒤 제자리로
    const float SwingTime = 0.12f, SwingBack = 0.22f, SwingArc = 85f;
    float swingT = 99f;
    float swingSign = 1f;

    // 무기마다: 반동 거리, 들리는 각도, 손에서 총구까지 길이
    float Recoil(int id) => id == -1 ? baseRecoil : id switch
    {
        SpecialAbilities.ShotgunId => 0.45f, SpecialAbilities.SniperId => 0.6f, SpecialAbilities.DualId => 0.15f,
        SpecialAbilities.FlameId => 0.05f, SpecialAbilities.SeekerId => 0.3f, SpecialAbilities.ChainId => 0.2f,
        SpecialAbilities.ScytheId => 0f, SpecialAbilities.GrenadeId => 0.5f, _ => 0.18f,
    };
    // 기본 무기 (-1): 캐릭터마다 다름 — 검은 크게 휘두르고, 표창 · 플라스크는 던지는 손짓
    static float BaseRecoil() => CharacterData.Selected switch
    {
        CharacterId.Swordsman => 0f, CharacterId.Rogue => 0.1f, CharacterId.Archer => 0.15f, CharacterId.Alchemist => 0.1f, _ => 0.18f,
    };
    static float BaseLift() => CharacterData.Selected switch
    {
        CharacterId.Swordsman => 0f, CharacterId.Rogue => 45f, CharacterId.Archer => 6f, CharacterId.Alchemist => 75f, _ => 8f,
    };

    float Lift(int id) => id == -1 ? baseLift : id switch
    {
        SpecialAbilities.ShotgunId => 22f, SpecialAbilities.SniperId => 16f, SpecialAbilities.GrenadeId => 26f,
        SpecialAbilities.SeekerId => 12f, SpecialAbilities.FlameId => 2f, SpecialAbilities.ScytheId => -70f, _ => 8f,
    };

    // 1.0.4~ 플레이어는 모든 이펙트(스킬 · 필살기 · 보스 패턴 · 경고 · 탄 · 장판 등) 위에: 맨 위 정렬 레이어 "Player" (ProjectSettings/TagManager, HPpack 다음)
    // 몸(모든 영웅 · 스킨 · 윤곽선은 같은 그림의 재질) · 들고 있는 무기 · 동작 인형 · 머리 위 막대 · 시약 표시 · 알림 글자가 이 레이어
    // 적은 Character 레이어 그대로(플레이어 아래), 화면 UI · 가장자리 번쩍임 · 페이드는 Screen Space Overlay 캔버스라 지금처럼 그 위
    static bool topLayerFound;
    static int topLayer;
    public static int TopLayer
    {
        get
        {
            if (topLayerFound) return topLayer;
            int id = SortingLayer.NameToID("Player");
            topLayer = SortingLayer.IsValid(id) ? id : SortingLayer.NameToID("HPpack");      // 레이어가 없으면 지금 있는 것 중 맨 위
            topLayerFound = true;
            return topLayer;
        }
    }

    public static void Attach(PlayerController p)
    {
        if (p == null) return;
        // 무기(HeldWeapon) · 동작 인형(PlayerMotion)은 몸의 레이어를 따라가므로 PlayerLook 을 붙이기 전에 몸부터 맨 위로
        if (p.TryGetComponent(out SpriteRenderer sr)) sr.sortingLayerID = TopLayer;
        if (p.GetComponent<PlayerLook>() != null) return;
        p.gameObject.AddComponent<PlayerLook>();
    }

    void Awake()
    {
        Instance = this;
        player = GetComponent<PlayerController>();
        body = GetComponent<SpriteRenderer>();
        GameObject go = new GameObject("HeldWeapon");
        go.transform.SetParent(transform, false);
        held = go.AddComponent<SpriteRenderer>();
        if (body != null)
        {
            held.sortingLayerID = body.sortingLayerID;
            held.sortingOrder = body.sortingOrder + 1;
        }
        held.enabled = false;
        foreach (Sprite s in Resources.LoadAll<Sprite>("Characters/" + SkinData.BodySheet("gunner_nogun"))) noGun[s.name] = s;     // 캐릭터 스킨
    }

    // 최적화 29: 영웅별 상수는 판 시작에 한 번 (CharacterData.Selected 는 부를 때마다 저장값 조회)
    float baseRecoil, baseLift;
    bool swordsman;
    readonly System.Collections.Generic.Dictionary<Sprite, Sprite> bareOf = new System.Collections.Generic.Dictionary<Sprite, Sprite>();

    void Start()
    {
        baseRecoil = BaseRecoil();
        baseLift = BaseLift();
        swordsman = CharacterData.Selected == CharacterId.Swordsman;
        FootShadow.Player(GetComponent<PlayerController>(), CharacterData.Current.color);     // 1단계-11~ 발밑 접촉 원
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // -1 = 기본 권총, -2 = 빈손 (낫을 던진 동안)
    int CurrentWeapon => player != null && player.special != null ? player.special.HeldWeapon : -1;

    void LateUpdate()
    {
        if (player == null || body == null) return;
        // 캐릭터 그림에 그려진 권총은 지우고 (애니메이션이 이번 프레임에 고른 그림을 같은 이름의 총 없는 그림으로)
        //   최적화 29: sprite.name 은 부를 때마다 새 문자열이라, 처음 보는 그림만 이름으로 찾고 그림 참조로 기억
        Sprite cur = body.sprite;
        if (cur != null)
        {
            if (!bareOf.TryGetValue(cur, out Sprite bare))
            {
                noGun.TryGetValue(cur.name, out bare);
                bareOf[cur] = bare;
            }
            if (bare != null && bare != cur) body.sprite = bare;
        }

        int id = CurrentWeapon;
        if (id != shownWeapon)
        {
            shownWeapon = id;
            // 무기 스킨이 있으면 스킨 그림 (없으면 원래 그림)
            Sprite baseHeld = Resources.Load<Sprite>(SkinData.HeldSprite(CharacterData.Current.held ?? "pistol"))
                              ?? Resources.Load<Sprite>("Weapons/weapon_" + (CharacterData.Current.held ?? "pistol"));
            // 다른 캐릭터의 특수 무기는 그림이 따로 없으면 기본 무기를 그대로 듦
            held.sprite = id >= 0 ? (Resources.Load<Sprite>("Weapons/weapon_" + id) ?? baseHeld)
                        : id == -1 ? baseHeld : null;
            // 진화한 무기는 그림이 따로 없으니 무기 스킨 색을 살짝 입힘
            held.color = id >= 0 && SkinFx.WeaponSkin != null ? SkinFx.Tint(Color.white) : Color.white;
        }
        // 죽는 연출 · 숨김(시작 연출) 중에는 몸과 함께 숨김
        bool show = held.sprite != null && body.enabled;
        if (held.enabled != show) held.enabled = show;
        if (!show) return;

        if (player.special != null) aim = player.special.AimDirection;
        if (aim.sqrMagnitude < 0.01f) aim = body.flipX ? Vector2.left : Vector2.right;
        // 몸도 겨누는 쪽을 바라봄 (총은 왼쪽, 몸은 오른쪽을 보는 어색함이 없게)
        if (Mathf.Abs(aim.x) > 0.05f) body.flipX = aim.x < 0f;

        float dt = Time.unscaledDeltaTime;
        kick = Mathf.MoveTowards(kick, 0f, dt * 5f);
        kickAngle = Mathf.MoveTowards(kickAngle, 0f, dt * 260f);

        bool left = aim.x < 0f;
        float swing = SwingOffset(dt, out float grow);
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + (left ? -(kickAngle + swing) : kickAngle + swing);
        Vector3 hand = new Vector3(left ? -0.35f : 0.35f, -0.55f, 0f);
        // 최적화 29: 값이 바뀌었을 때만 씀 (가만히 겨누고 있으면 쓰지 않음)
        Transform ht = held.transform;
        Vector3 pos = hand - (Vector3)(aim * (kick + 0.25f * drawAmount));
        Vector3 scale = Vector3.one * (grow * HeldScale(id) * (1f + 0.25f * drawAmount));
        if (pos != shownPos || angle != shownAngle || scale != shownScale)
        {
            shownPos = pos; shownAngle = angle; shownScale = scale;
            ht.localPosition = pos;
            ht.localRotation = Quaternion.Euler(0f, 0f, angle);
            ht.localScale = scale;
        }
        if (held.flipY != left) held.flipY = left;   // 왼쪽을 볼 때 무기가 뒤집히지 않게
        Color bc = body.color;
        if (held.color != bc) held.color = bc;       // 피격 깜빡임을 몸과 같이
    }

    Vector3 shownPos = new Vector3(float.NaN, 0f, 0f), shownScale;
    float shownAngle = float.NaN;

    // 기본 무기 크기: 검사의 장검은 크게 (레벨업 긴 칼날로 더 커짐)
    float HeldScale(int id)
    {
        if (id != -1 || !swordsman) return 1f;
        CharacterKit kit = CharacterKit.Instance;
        return 2.6f * (kit != null ? Mathf.Sqrt(kit.reachMul) : 1f);
    }

    // 휘두르는 중이면 겨눈 방향에서 벗어난 각도, 휘두르는 순간 검이 조금 커짐
    float SwingOffset(float dt, out float grow)
    {
        grow = 1f;
        if (swingT > SwingTime + SwingBack) return 0f;
        swingT += dt;
        if (swingT < SwingTime)
        {
            float k = swingT / SwingTime;
            k = 1f - (1f - k) * (1f - k);                  // 처음에 빠르게
            grow = 1f + 0.5f * Mathf.Sin(k * Mathf.PI);
            return swingSign * Mathf.Lerp(SwingArc, -SwingArc, k);
        }
        float b = Mathf.Clamp01((swingT - SwingTime) / SwingBack);
        return swingSign * Mathf.Lerp(-SwingArc, 0f, Mathf.SmoothStep(0f, 1f, b));
    }

    // 궁수: 시위를 당긴 정도 (0~1) — 활이 몸 쪽으로 당겨지고 커짐
    float drawAmount;
    public static void Draw(float k)
    {
        if (Instance != null) Instance.drawAmount = k;
    }

    // 검사 평타: alt 이면 아래→위로
    public static void Swing(bool alt)
    {
        if (Instance == null) return;
        Instance.swingT = 0f;
        Instance.swingSign = alt ? 1f : -1f;
    }

    // 총구 끝 (들고 있는 무기 그림의 앞쪽 끝): 총알도 여기서 나감
    public bool HasTip => held != null && held.enabled && held.sprite != null;
    public Vector3 TipPosition => Tip();

    Vector3 Tip()
    {
        if (!HasTip) return player.BaseMuzzle;
        float len = held.sprite.bounds.size.x * (1f - held.sprite.pivot.x / held.sprite.rect.width);
        return held.transform.position + (Vector3)(aim * len);
    }

    // 무기를 쏠 때 (SpecialAbilities · PlayerController 가 부름). id = -1 이면 기본 권총
    public static void Fired(int id)
    {
        if (Instance != null) Instance.OnFired(id);
    }

    void OnFired(int id)
    {
        kick = Recoil(id);
        kickAngle = Lift(id);
        Vector3 tip = Tip();
        float rot = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        Vector2 back = -aim;
        Vector2 side = new Vector2(-aim.y, aim.x);
        switch (id)
        {
            case SpecialAbilities.ShotgunId:
                // 불티가 부채꼴로 튀고 연기
                for (int i = 0; i < 6; i++)
                {
                    Vector2 d = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f)) * aim;
                    FlameParticle.Spawn(Hostile.Glow, tip, d * Random.Range(5f, 9f), Random.Range(0.18f, 0.3f), 0.06f, 0.02f, false);
                }
                Fx.Spawn("fx_smoke", tip, 1.6f, new Color(0.6f, 0.55f, 0.5f, 0.6f), 16f);
                Eject(new Color(0.85f, 0.25f, 0.15f));
                break;
            case SpecialAbilities.SniperId:
                // 청록 고리가 총구에서 퍼지고 긴 섬광
                Fx.Spawn("fx_shock", tip, 1.8f, new Color(0.5f, 0.95f, 1f, 0.9f), 24f);
                Fx.Spawn("fx_trail_dot", tip + (Vector3)(aim * 0.6f), 1f, new Color(0.8f, 1f, 1f), 1f, rot, 16, false, 0.08f);
                Eject(new Color(0.6f, 0.9f, 1f));
                break;
            case SpecialAbilities.DualId:
                Eject(new Color(1f, 0.85f, 0.4f));
                Fx.Spawn("fx_sparkle", tip, 0.7f, new Color(1f, 0.9f, 0.5f), 24f, 0f, 16);
                break;
            case SpecialAbilities.FlameId:
                FlameParticle.Spawn(Hostile.Glow, tip, aim * 3f + side * Random.Range(-1f, 1f), 0.25f, 0.05f, 0.12f, false);
                break;
            case SpecialAbilities.SeekerId:
                Fx.Spawn("fx_orb", tip, 1.2f, new Color(0.8f, 0.6f, 1f), 20f);
                Fx.Spawn("fx_sparkle", tip + (Vector3)(side * 0.3f), 0.6f, new Color(0.85f, 0.7f, 1f), 18f, 0f, 16);
                break;
            case SpecialAbilities.ChainId:
                Fx.Spawn("fx_markbolt", tip, 1f, new Color(0.7f, 0.95f, 1f), 24f, Random.Range(-20f, 20f), 16);
                break;
            case SpecialAbilities.ScytheId:
                // 던지는 동작: 낫이 한 바퀴 휘둘러짐 (손에 든 낫은 잠깐 사라졌다 돌아옴)
                Fx.Spawn("fx_slash", transform.position + (Vector3)(aim * 0.8f), 2.4f, new Color(0.85f, 0.6f, 1f), 26f, rot, 16);
                break;
            case SpecialAbilities.GrenadeId:
                Fx.Spawn("fx_smoke", tip, 2f, new Color(0.45f, 0.35f, 0.3f, 0.7f), 14f);
                FlameParticle.Spawn(Hostile.Glow, tip, aim * 4f, 0.25f, 0.08f, 0.02f, false);
                break;
            default:
                // 탄피는 총만 (다른 캐릭터의 검 · 표창 · 활 · 플라스크와 그 특수 무기는 없음)
                if (id >= 20 || (id == -1 && !CharacterData.IsGunner)) break;
                Eject(new Color(0.95f, 0.8f, 0.4f));
                break;
        }
    }

    // 탄피가 옆으로 튀어 떨어짐
    void Eject(Color c)
    {
        Vector3 at = held != null && held.enabled ? held.transform.position : player.BaseMuzzle;
        Vector2 side = new Vector2(-aim.y, aim.x) * (aim.x < 0f ? -1f : 1f);
        Casing.Spawn(at, (side * 2.5f + Vector2.up * 3f) + Random.insideUnitCircle * 0.8f, c);
    }
}

// 튀어나와 떨어지며 사라지는 탄피 (작은 도트)
public class Casing : MonoBehaviour
{
    Vector2 v;
    float age;
    SpriteRenderer sr;

    // 최적화 16: 다 떨어진 탄피는 지우지 않고 꺼 두었다가 다시 씀 (발사마다 새로 만들고 지우지 않게)
    static readonly System.Collections.Generic.Stack<Casing> pool = new System.Collections.Generic.Stack<Casing>();
    static int effectLayer = int.MinValue;
    int spawnFrame = -1;

    public static void Spawn(Vector3 pos, Vector2 velocity, Color color)
    {
        Casing c = null;
        while (pool.Count > 0 && c == null) c = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        if (c == null)
        {
            GameObject go = SpecialAbilities.MakeSprite("Casing", SpecialAbilities.GlowSprite, pos, 0.03f, color, "Effect", 6);
            c = go.AddComponent<Casing>();
            c.v = velocity;
            c.sr = go.GetComponent<SpriteRenderer>();
            return;
        }
        // MakeSprite 로 막 만든 것과 같은 상태로
        if (effectLayer == int.MinValue) effectLayer = SortingLayer.NameToID("Effect");
        Transform t = c.transform;
        t.SetPositionAndRotation(pos, Quaternion.identity);
        t.localScale = Vector3.one * 0.03f;
        c.sr.sprite = SpecialAbilities.GlowSprite;
        c.sr.color = color;
        c.sr.sortingLayerID = effectLayer;
        c.sr.sortingOrder = 6;
        c.v = velocity;
        c.age = 0f;
        c.spawnFrame = Time.frameCount;
        c.gameObject.SetActive(true);
    }

    void Update()
    {
        // 새로 만든 오브젝트처럼, 켜진 그 프레임에는 움직이지 않음
        if (Time.frameCount == spawnFrame) return;
        age += Time.deltaTime;
        v.y -= 14f * Time.deltaTime;
        transform.position += (Vector3)(v * Time.deltaTime);
        transform.Rotate(0f, 0f, 720f * Time.deltaTime);
        if (sr != null) { Color c = sr.color; c.a = Mathf.Clamp01(1f - (age - 0.25f) / 0.2f); sr.color = c; }
        if (age > 0.45f)
        {
            if (sr == null) { Destroy(gameObject); return; }
            gameObject.SetActive(false);
            pool.Push(this);
        }
    }
}
