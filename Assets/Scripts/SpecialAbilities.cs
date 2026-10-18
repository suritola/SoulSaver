using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialKind { Weapon, Skill, Passive }

// 특수 능력 하나의 정의
[System.Serializable]
public class SpecialDef
{
    public string name;
    public SpecialKind kind;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
}

// 지옥 입장 때 고르는 특수 능력 (무기 / 스킬 / 패시브)
public partial class SpecialAbilities : MonoBehaviour
{
    public const int ShotgunId = 0, SniperId = 1, DualId = 2, FlameId = 3, SeekerId = 4, ChainId = 5, ScytheId = 6, GrenadeId = 7;
    public const int DashId = 8, FireZoneId = 9, TimeWarpId = 10, PactId = 11, SoulBurstId = 12, SkeletonsId = 13, MirrorId = 14, HookId = 15;
    public const int OrbsId = 16, ThornsId = 17, CurseId = 18, UndyingId = 19;

    [Header("능력 목록 (순서 = ID)")]
    public SpecialDef[] abilities;

    [Header("효과용 스프라이트")]
    public Sprite glowSprite;       // 폭발, 장판, 빛
    public Sprite swirlSprite;      // 영혼 구체
    public Sprite allySprite;       // 아군 해골
    public Sprite panelSprite;
    public Sprite barFillSprite;
    public TMP_FontAsset font;
    public Material fontMaterial;

    // 고른 능력들 (최대 3개)
    readonly List<int> equipped = new List<int>();
    readonly List<int> weapons = new List<int>();
    readonly List<int> skills = new List<int>();
    public IReadOnlyList<int> EquippedIds => equipped;
    public IReadOnlyList<int> Weapons => weapons;
    public int SkillCount => skills.Count;
    public const int MaxSkills = 3;

    // 진화한 능력: 특수 능력 포인트로 이미 가진 능력을 한 번 더 고르면 진화
    readonly HashSet<int> evolved = new HashSet<int>();
    public bool IsEvolved(int id) => evolved.Contains(id);

    // 피해 출처 이름(SourceOf)이 진화한 능력인지 (Fx 단계 이펙트용)
    public bool IsEvolvedSource(string source)
    {
        if (string.IsNullOrEmpty(source) || abilities == null) return false;
        foreach (int id in evolved)
            if (id >= 0 && id < abilities.Length && abilities[id] != null && abilities[id].name == source) return true;
        return false;
    }

    // 무기 강화 (2장 상점): 무기마다 [피해, 속도, 특성] 단계
    public const int StatDamage = 0, StatRate = 1, StatMag = 2, StatTrait = 3;
    public static readonly int[] WeaponStatMax = { 5, 5, 3, 3 };
    readonly Dictionary<int, int[]> weaponLevels = new Dictionary<int, int[]>();

    // 스킬 키: 고른 순서대로 스킬 1 · 2 · 3 (기본 E, F, C · 설정에서 바꿀 수 있음)
    static readonly GameAction[] SkillKeys = { GameAction.Skill1, GameAction.Skill2, GameAction.Skill3 };

    // 무기: Q로 기본 권총 → 무기1 → 무기2 순서로 교체 (-1 = 기본 권총)
    int weaponIndex = -1;
    public bool WeaponActive => weaponIndex >= 0 && weaponIndex < weapons.Count;
    public int CurrentWeapon => WeaponActive ? weapons[weaponIndex] : -1;
    // 손에 들고 있는 무기 (PlayerLook): -1 = 기본 권총, 낫을 던진 동안은 빈손(-2)
    public int HeldWeapon => CurrentWeapon == ScytheId && activeScythe != null ? -2 : CurrentWeapon;

    PlayerController player;
    float nextFire;
    readonly Dictionary<int, float> cooldownUntil = new Dictionary<int, float>();
    readonly Dictionary<int, float> cooldownLength = new Dictionary<int, float>();
    int souls;
    int undyingUses;
    float heat;
    bool overheated;
    float sniperCharge = -1f;
    bool dualToggle;
    GameObject activeScythe;
    readonly List<Transform> orbs = new List<Transform>();
    readonly Dictionary<Collider2D, float> orbHitTimes = new Dictionary<Collider2D, float>();
    Material lineMaterial;

    // 피드백 (소리, 조준 표시, 알림 글자)
    SpecialFeedback fx;
    LineRenderer aimLine;       // 조준 점선
    LineRenderer previewRing;   // 범위 원
    LineRenderer previewCone;   // 산탄총 부채꼴
    LineRenderer targetRing;    // 유도탄/갈고리 표적
    LineRenderer auraRing;      // 시간 왜곡 범위
    GameObject chargeGlow;      // 저격 충전 빛
    GameObject pactAura;        // 희생의 계약 붉은 기운
    GameObject curseGlow;       // 저주탄 장전 표시
    bool chargeFullPlayed;
    bool flameWasFiring;
    GameObject flameMuzzle;
    int aimingSkill = -1;       // 누르고 있는 조준형 스킬
    int lastBullets = -1;
    float lastOrbSound;
    float timeWarpUntil;
    readonly Dictionary<int, bool> wasReady = new Dictionary<int, bool>();
    readonly Dictionary<int, float> rowFlashUntil = new Dictionary<int, float>();

    // 누르고 있는 동안 미리보기를 보여주고 떼면 쓰는 스킬
    static bool IsAimedSkill(int id) => id == DashId || id == FireZoneId || id == HookId;

    // 적 · 보스 스킬이 같이 쓰는 빛 스프라이트와 소리
    public static Sprite GlowSprite;
    public static Sprite SwirlSprite;
    public static SpecialFeedback SharedFx;
    public static SpecialAbilities SharedInstance { get; private set; }

    // 그림자 대시 거리: 이동 속도에 비례 (기본 속도 20 → 6칸)
    float DashDistance => player.speed * 0.3f;

    void Awake()
    {
        GlowSprite = glowSprite;
        SwirlSprite = swirlSprite;
        // 씬을 다시 불러와도 정적 상태가 남지 않도록
        EnermyController.GlobalSpeedMultiplier = 1f;
        EnermyController.Decoy = null;
    }

    void Start()
    {
        KitExtendAbilities();                   // 거너가 아닌 캐릭터의 능력 (SpecialAbilities.Kits.cs)
        player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.special = this;
        EnermyController.Killed += OnEnemyKilled;
        EnermyController.DamageHook = KitDamageHook;
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        BuildHud();

        Burn.FireSprite = glowSprite;
        fx = gameObject.AddComponent<SpecialFeedback>();
        fx.Init(font, fontMaterial);
        SharedFx = fx;
        SharedInstance = this;
        aimLine = fx.NewLine("AimLine", true);
        previewRing = fx.NewLine("PreviewRing", true);
        previewCone = fx.NewLine("PreviewCone", true);
        targetRing = fx.NewLine("TargetRing", false, 21);
        auraRing = fx.NewLine("AuraRing", true, 19);
    }

    void OnDestroy()
    {
        EnermyController.Killed -= OnEnemyKilled;
        if (EnermyController.DamageHook == (System.Func<EnermyController, float, float>)KitDamageHook) EnermyController.DamageHook = null;
        EnermyController.GlobalSpeedMultiplier = 1f;
        EnermyController.Decoy = null;
    }

    public void Equip(IEnumerable<int> ids)
    {
        bool hadWeapons = weapons.Count > 0;
        List<int> newSkills = new List<int>();
        foreach (int id in ids)
        {
            if (id < 0 || id >= abilities.Length || equipped.Contains(id)) continue;
            equipped.Add(id);
            if (abilities[id].kind == SpecialKind.Weapon) weapons.Add(id);
            if (abilities[id].kind == SpecialKind.Skill) { skills.Add(id); newSkills.Add(id); }
            if (id == OrbsId) SpawnOrbs(3);
            KitOnEquip(id, false);
        }
        if (weapons.Count > 0 && !UsesEvolution) Hints.Show("swap", "[{SWAP}]로 특수 무기와 기본 무기를 바꿔 듭니다. [{RELOAD}]는 장전입니다.");
        if (skills.Count > 0) Hints.Show("skill", "스킬은 [{SKILL1}] · [{SKILL2}] · [{SKILL3}]로 씁니다. 쿨타임은 왼쪽 아래 칸에 보입니다.");
        // 처음 무기를 골랐다면 바로 꺼내 들고 시작 (이미 들고 있던 무기는 그대로)
        if (!hadWeapons) weaponIndex = weapons.Count > 0 ? 0 : -1;
        RebuildHudRows();
        foreach (int id in newSkills) AnnounceKey(id, false);      // 1.1.2~ 어느 키로 쓰는지 화면 가운데 + 칸 반짝임 (SpecialAbilities.KeyGuide)
    }

    public bool Has(int id) => equipped.Contains(id);

    // 가진 무기 중 하나를 바로 꺼내 듦 (-1 = 기본 권총) · 트레일러 촬영용
    public void SelectWeapon(int id)
    {
        weaponIndex = weapons.IndexOf(id);
        CancelSniperCharge();
        if (flameMuzzle != null) Destroy(flameMuzzle);
        flameWasFiring = false;
        fx.StopLoop();
    }

    // ================================================================= weapon ammo (무기마다 따로)
    class WeaponAmmo
    {
        public int ammo;
        public float reloadEnd = -1f;
        public bool Reloading => reloadEnd > 0f;
    }
    readonly Dictionary<int, WeaponAmmo> ammoOf = new Dictionary<int, WeaponAmmo>();

    // 기본 탄창 (0 = 탄약 없음: 화염 방사기는 열기, 낫은 회수)
    static int BaseMag(int id) => id switch
    {
        ShotgunId => 5, SniperId => 4, DualId => 24, SeekerId => 12, ChainId => 8, GrenadeId => 6, _ => KitMag(id),
    };
    // 발사 간격 (초) — 권총의 공격 속도 강화와는 별개
    static float BaseInterval(int id) => id switch
    {
        ShotgunId => 0.65f, SniperId => 0.8f, DualId => 0.22f, SeekerId => 0.4f, ChainId => 0.45f, GrenadeId => 0.8f, _ => KitInterval(id),
    };
    static float BaseReload(int id) => id switch
    {
        ShotgunId => 2f, SniperId => 2.2f, DualId => 1.8f, SeekerId => 1.6f, ChainId => 1.8f, GrenadeId => 2.4f, _ => KitReload(id),
    };

    public static bool UsesAmmo(int id) => BaseMag(id) > 0;
    public int MagSize(int id) => Mathf.RoundToInt(BaseMag(id) * (1f + 0.25f * WeaponLevel(id, StatMag)) * TreeMagMul);

    WeaponAmmo Ammo(int id)
    {
        if (!ammoOf.TryGetValue(id, out WeaponAmmo a))
        {
            a = new WeaponAmmo { ammo = MagSize(id) };
            ammoOf[id] = a;
        }
        return a;
    }

    bool WeaponHasAmmo(int id)
    {
        if (!UsesAmmo(id)) return true;
        WeaponAmmo a = Ammo(id);
        return !a.Reloading && a.ammo > 0;
    }

    void StartWeaponReload(int id)
    {
        if (!UsesAmmo(id)) return;
        WeaponAmmo a = Ammo(id);
        if (a.Reloading || a.ammo >= MagSize(id)) return;
        a.reloadEnd = Time.time + BaseReload(id) * TreeReloadMul;
        fx.Play(ReloadSound(id), 0.8f, Random.Range(0.96f, 1.04f));
        ReloadShockwave(player.transform.position);
        // 2.1.8: 특수 무기 장전에도 장전 고유 스킬이 터짐 (탄피 지뢰 · 빈 주머니의 비 …, 예전엔 권총 장전에만)
        if (id == CurrentWeapon) SignatureSkills.ReloadStart();
    }

    // ---------------- 고유 스킬이 쓰는 "지금 들고 있는 무기"의 탄창 (2.1.8~, 특수 무기를 들었으면 그 무기, 아니면 기본 무기)
    // 들고 있는 특수 무기가 장전 중인지 (달리며 장전)
    public bool HeldReloading => WeaponActive && UsesAmmo(CurrentWeapon) && Ammo(CurrentWeapon).Reloading;

    // 탄을 돌려줌 (전리품 탄약 · 귀환하는 칼날): 특수 무기를 들었으면 그 탄창에, 아니면 false
    public bool RefundHeldAmmo(int n)
    {
        if (!WeaponActive || !UsesAmmo(CurrentWeapon)) return false;
        WeaponAmmo a = Ammo(CurrentWeapon);
        if (!a.Reloading) a.ammo = Mathf.Min(MagSize(CurrentWeapon), a.ammo + n);
        return true;
    }

    // 탄창을 가득 (무법자)
    public bool RefillHeldAmmo()
    {
        if (!WeaponActive || !UsesAmmo(CurrentWeapon)) return false;
        WeaponAmmo a = Ammo(CurrentWeapon);
        a.reloadEnd = -1f;
        a.ammo = MagSize(CurrentWeapon);
        return true;
    }

    // 속사 장전: 다음 발을 바로 쏨
    public void QuickFire() => nextFire = Time.time + 0.05f;

    // 무기마다 다른 장전 소리 (SpecialFeedback 이 만든 소리 이름, -1 = 캐릭터 기본 무기)
    public static string ReloadSound(int id) => id switch
    {
        ShotgunId => "rl_shotgun", SniperId => "rl_sniper", DualId => "rl_dual", SeekerId => "rl_seeker",
        ChainId => "rl_chain", GrenadeId => "rl_grenade",
        KitBlowgun => "rl_blowgun", KitCards => "rl_cards", KitNetBow => "rl_netbow", KitBurstBow => "rl_burstbow",
        KitQuicksilver => "rl_quicksilver", KitMagnet => "rl_magnet", KitFirework => "rl_firework",
        _ => "rl_stars",
    };

    // 들고 있지 않은 무기도 장전은 계속 진행
    void UpdateWeaponReloads()
    {
        foreach (int id in weapons)
        {
            if (!UsesAmmo(id)) continue;
            WeaponAmmo a = Ammo(id);
            if (a.Reloading && Time.time >= a.reloadEnd)
            {
                a.reloadEnd = -1f;
                a.ammo = MagSize(id);
                if (id == CurrentWeapon) { fx.Play("clank", 0.4f, 1.3f); SignatureSkills.ReloadEnd(); }     // 섬광 장전 · 속사 장전 · 천둥 장전 (2.1.8)
            }
        }
    }

    // 탄창 글 (값이 그대로면 전에 만든 글을 다시 씀 — 매 프레임 문자열을 만들지 않게)
    int ammoTextId = int.MinValue, ammoTextKey;
    Loc.Lang ammoTextLang;
    string ammoTextCache;

    string AmmoText(int id)
    {
        if (!UsesAmmo(id)) return null;
        WeaponAmmo a = Ammo(id);
        int key = a.Reloading ? -1 - (int)(Time.unscaledTime / 0.3f) % 3 : a.ammo * 10000 + MagSize(id);
        if (id == ammoTextId && key == ammoTextKey && ammoTextLang == Loc.Current && ammoTextCache != null) return ammoTextCache;
        ammoTextId = id;
        ammoTextKey = key;
        ammoTextLang = Loc.Current;
        ammoTextCache = a.Reloading ? Loc.T("장전 중") + new string('.', (int)(Time.unscaledTime / 0.3f) % 3 + 1) : a.ammo + " / " + MagSize(id);
        return ammoTextCache;
    }

    // ================================================================= scythe sprite (코드로 그린 낫)
    static Sprite scytheSprite;
    static Sprite ScytheSprite => scytheSprite != null ? scytheSprite : (scytheSprite = MakeScytheSprite());

    static Sprite MakeScytheSprite()
    {
        const int S = 48;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[S * S];
        bool[] solid = new bool[S * S];
        void Put(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= S || y >= S) return;
            px[y * S + x] = c;
            solid[y * S + x] = true;
        }

        // 자루: 왼쪽 아래에서 오른쪽 위로
        for (float t = 0f; t <= 1f; t += 0.01f)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(9f, 33f, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(5f, 37f, t));
            Color wood = Color.Lerp(new Color(0.32f, 0.2f, 0.14f), new Color(0.5f, 0.34f, 0.22f), (x + y) % 5 == 0 ? 1f : 0.3f);
            Put(x, y, wood);
            Put(x + 1, y, wood * 0.85f + new Color(0f, 0f, 0f, 0.15f));
        }
        // 자루 끝 장식
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                Put(9 + dx, 5 + dy, new Color(0.6f, 0.45f, 0.8f));

        // 날: 자루 끝에서 왼쪽 아래로 휘어지는 초승달
        Vector2 outer = new Vector2(24f, 30f);
        Vector2 inner = new Vector2(20f, 25f);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dOut = Vector2.Distance(p, outer);
                float dIn = Vector2.Distance(p, inner);
                if (dOut > 16f || dIn < 15f || p.y < 27f || p.x > 38f) continue;
                // 바깥 가장자리일수록 밝게 빛나는 날
                float edge = Mathf.Clamp01((dOut - 12f) / 4f);
                Color c = Color.Lerp(new Color(0.42f, 0.2f, 0.62f), new Color(0.93f, 0.88f, 1f), edge);
                Put(x, y, c);
            }
        }

        // 어두운 외곽선
        Color line = new Color(0.12f, 0.04f, 0.18f, 1f);
        Color[] result = (Color[])px.Clone();
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                if (solid[y * S + x]) continue;
                bool near = false;
                for (int k = 0; k < 4 && !near; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    near = nx >= 0 && ny >= 0 && nx < S && ny < S && solid[ny * S + nx];
                }
                result[y * S + x] = near ? line : new Color(0f, 0f, 0f, 0f);
            }
        }
        tex.SetPixels(result);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 24f);
    }

    // ================================================================= weapon volley (타겟팅 스킬 일제 사격)
    static Color WeaponColor(int id) => id switch
    {
        ShotgunId => new Color(1f, 0.55f, 0.2f),
        SniperId => new Color(0.5f, 0.95f, 1f),
        DualId => new Color(1f, 0.85f, 0.5f),
        FlameId => new Color(1f, 0.5f, 0.15f),
        SeekerId => new Color(0.75f, 0.5f, 1f),
        ChainId => new Color(0.6f, 0.9f, 1f),
        ScytheId => new Color(0.8f, 0.55f, 1f),
        GrenadeId => new Color(1f, 0.45f, 0.1f),
        _ => KitColor(id),
    };

    static bool Alive(EnermyController e) => e != null && !e.IsDead;

    // 우클릭 타겟팅 스킬: 들고 있는 무기마다 전혀 다른 형식의 필살기
    static string VolleyName(int id) => id switch
    {
        ShotgunId => Loc.T("지옥불 포격"),
        SniperId => Loc.T("관통 레일건"),
        DualId => Loc.T("총알 폭풍"),
        FlameId => Loc.T("화염 회오리"),
        SeekerId => Loc.T("영혼 떼"),
        ChainId => Loc.T("뇌운"),
        ScytheId => Loc.T("죽음의 춤"),
        GrenadeId => Loc.T("용암 융단폭격"),
        _ => Loc.T("일제 사격"),
    };

    // 1.3.3~ 뇌운 (사슬 번개 필살기): 일제 사격과 따로 도는 번개 폭풍 — 플레이어는 그동안 평타를 쏠 수 있음
    //   이 컴포넌트의 코루틴이라 플레이어 · 씬이 사라지면 같이 멈춤 · 끝이 정해진 반복 (storm 초)
    IEnumerator ThunderCloud(List<EnermyController> alive, float dmg, Color c, float storm)
    {
        FxAnim cloud = Fx.Play("fx_cloud", player.transform.position + Vector3.up * 6f, 12f, new Color(0.35f, 0.4f, 0.55f, 0.8f), 3f, 0f, 25, true, storm + 0.1f);
        if (cloud != null) cloud.follow = player.transform;
        for (float t = 0f; t < storm; t += 0.15f)
        {
            if (player == null) yield break;
            alive.RemoveAll(e => !Alive(e));
            Transform target = alive.Count > 0 ? alive[Random.Range(0, alive.Count)].transform
                                               : Specials.NearestEnemy(player.transform.position + (Vector3)(Random.insideUnitCircle * 6f), 14f);
            if (target != null)
            {
                Vector3 at = target.position;
                Fx.Bolt(at + new Vector3(Random.Range(-2f, 2f), 14f), at, 1.4f, c, 0.2f);
                Fx.Spawn("fx_shock", at, 3f, c, 24f);
                Specials.Damage(target.gameObject, dmg * 0.5f, Vector3.zero, 0f);
                Collider2D col = target.GetComponent<Collider2D>();
                if (col != null) ChainLightning(at, col, dmg * 0.25f, 1);
                fx.Play("thunder", 0.7f, Random.Range(0.9f, 1.2f));
                fx.Play("zap", 0.5f, Random.Range(0.8f, 1.1f));
                Fx.Spawn("fx_explosion", at, 2.5f, new Color(0.7f, 0.9f, 1f), 22f);
                fx.Shake(0.25f, 0.08f);
            }
            yield return new WaitForSeconds(0.15f);
        }
    }

    public IEnumerator WeaponVolley(List<EnermyController> targets, float baseDamage, float blood, int ultId = -99)
    {
        int id = ultId != -99 ? ultId : UltId;            // 진화한 무기의 필살기 (권총을 들고 있어도) · 거너 1차 · 2차를 따로 부를 때는 ultId
        float dmg = baseDamage * WeaponDamageMul(id) * UltPower(id);
        if (id != DualId) dmg *= SignatureSkills.SoulUltMul;      // 총알 폭풍은 총알마다 영혼탄이 붙음 (ApplyGunCards)
        Color c = WeaponColor(id);
        fx.FloatText(player.transform.position, VolleyName(id) + "!", c, 6f, 0f);
        fx.Play("pulse", 0.6f, 1.4f);
        List<EnermyController> alive = targets.FindAll(Alive);

        switch (id)
        {
            case ShotgunId:
                // 지옥불 포격: 마우스 쪽으로 거대한 부채꼴 폭발 3연발
                for (int blast = 0; blast < 3 + UltTrait(id); blast++)
                {
                    Vector3 start = player.MuzzlePosition;
                    Vector2 dir = AimDir();
                    player.FaceTowards(MouseWorld());
                    PlayerMotion.Of(player).Recoil(dir, 2f);            // 몸동작: 큰 반동 (1.0.1)
                    const float range = 11f, half = 35f;
                    using (WeakHit.AimScope(1.4f))          // 빈틈: 커서 쪽 부채꼴의 가운데 20° 만 (평타 샷건과 같은 Arc · 이 블록 안에 yield 없음)
                    using (var ovl = PhysHits.Circle(start, range)) foreach (Collider2D col in ovl)
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        Vector2 to = col.transform.position - start;
                        if (Vector2.Angle(dir, to) <= half) WeakHit.Damage(col.gameObject, WeakShape.Arc(start, dir, 10f, range, 1.4f), dmg * 0.9f, to.normalized, 3.5f);
                    }
                    for (int i = -2; i <= 2; i++)
                        for (int k = 1; k <= 3; k++)
                        {
                            Vector2 d = Quaternion.Euler(0, 0, i * half / 2.2f) * dir;
                            Fx.Spawn("fx_explosion", start + (Vector3)(d * range * k / 3.4f), 2.2f + k * 0.9f, Color.white, 14f + Random.Range(0f, 6f));
                        }
                    Fx.Spawn("fx_shock", start, 7f, c, 20f);
                    Fx.Spawn("fx_explosion", start + (Vector3)(dir * 2f), 5f, Color.white, 18f);
                    Fx.Spawn("fx_muzzle", start + (Vector3)(dir * 0.8f), 4f, c, 20f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 16);
                    fx.Play("shotgun", 1f, 0.9f - blast * 0.05f);
                    fx.Play("bigboom", 0.6f, 1.3f);
                    fx.Shake(0.55f, 0.18f);
                    yield return new WaitForSeconds(0.28f);
                }
                break;

            case SniperId:
                PlayerMotion.Of(player).Brace(0.5f, AimDir());       // 몸동작: 웅크려 충전 → 큰 반동
                // 관통 레일건: 충전 뒤 화면을 가로지르는 굵은 광선 (조준한 적이 많을수록 강함)
                {
                    float wide = 1f + 0.3f * UltTrait(id);
                    FxAnim aim = null;
                    fx.Play("railcharge", 0.9f, 1f);
                    for (float t = 0f; t < 0.5f; t += Time.deltaTime)
                    {
                        Vector3 m = player.MuzzlePosition;
                        // 충전 중에는 화면 끝까지 닿는 점선 조준선
                        PreviewLine(aimLine, m, m + (Vector3)(AimDir() * ScreenEdgeDistance(m, AimDir())), new Color(c.r, c.g, c.b, 0.5f + t), 0.1f + t * 0.3f);
                        Fx.Spawn("fx_orb", m, 0.8f + t * 2f, c, 20f);
                        yield return null;
                    }
                    if (aim != null) Destroy(aim.gameObject);
                    Vector3 from = player.MuzzlePosition;
                    Vector3 to = from + (Vector3)(AimDir() * 45f);
                    Fx.Beam(from, to, 2.4f * wide, c, 0.4f, 16);
                    Fx.Beam(from, to, 0.8f * wide, Color.white, 0.3f, 17);
                    // 광선 위의 적이 많을수록 강함
                    List<Collider2D> onLine = new List<Collider2D>();
                    using (var ovl = PhysHits.Circle((from + to) * 0.5f, 23f)) foreach (Collider2D col in ovl)
                    {
                        if (!col.CompareTag("enermy") && !col.CompareTag("boss")) continue;
                        if (Hostile.DistanceToSegment(col.transform.position, from, to) > 1.5f * wide) continue;
                        onLine.Add(col);
                    }
                    float power = 1.5f + 0.25f * onLine.Count;
                    using var railAim = WeakHit.AimScope(1.75f);      // 빈틈: 커서 쪽으로 곧게 나가는 광선 (광선 가운데 줄기 반폭 0.6 · 이 블록 뒤 yield 없음)
                    foreach (Collider2D col in onLine)
                    {
                        WeakHit.Damage(col.gameObject, WeakShape.Segment(from, to, 0.6f * wide, 1.75f), dmg * power, (to - from).normalized, 2f);
                        Fx.Spawn("fx_spark", col.transform.position, 2f, c, 20f);
                    }
                    Fx.Spawn("fx_shock", from, 6f, c, 22f);
                    for (float k = 4f; k < 45f; k += 5f)
                        Fx.Spawn("fx_shock", from + (to - from).normalized * k, 3.5f * wide, c, 18f + k * 0.3f);
                    Fx.Spawn("fx_explosion", to - (to - from).normalized * 2f, 4f, Color.white, 16f);
                    fx.Play("railgun", 1f, 1f);
                    fx.Play("crack", 0.8f, 0.6f);
                    fx.Shake(0.7f, 0.25f);
                }
                break;

            case DualId:
                PlayerMotion.Of(player).Spin(1.6f + 0.4f * UltTrait(id), 4f);     // 몸동작: 돌며 난사
                // 총알 폭풍: 1.6초 동안 두 줄기 나선으로 사방 난사 (움직이며 쓸 수 있음)
                // 2.2.0: 캐릭터 주위 StormRange 칸까지만 날아감 (영혼 트리 「폭풍 사거리」로 +3칸씩)
                {
                    float range = StormRange;
                    float angle = Random.Range(0f, 360f);
                    for (float t = 0f; t < 1.6f + 0.4f * UltTrait(id); t += 0.04f)
                    {
                        for (int arm = 0; arm < 2; arm++)
                        {
                            float a = (angle + arm * 180f) * Mathf.Deg2Rad;
                            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                            Bullet b = player.CreateBullet(player.transform.position + (Vector3)(d * 0.6f), d, dmg * 0.3f, player.pene, 0f, true, 0.4f);
                            if (b == null) continue;
                            // 모든 적을 꿰뚫음 (적마다 한 번씩)
                            b.pene = 9999;
                            b.hitOnce = new HashSet<int>();
                            if (b.TryGetComponent(out SpriteRenderer sr)) sr.color = c;
                            // 2.2.1: 총알 폭풍에도 총알 카드 · 고유 스킬 (영혼 탄환이면 푸른 영혼탄으로), 덤 총알 · 유도 · 폭발탄은 빼고
                            SignatureSkills.UltShots = true;
                            ApplyGunCards(b, true, false);
                            SignatureSkills.UltShots = false;
                            b.lifetime = Mathf.Max(0.05f, (range - 0.6f) / Mathf.Max(1f, b.speed));     // 카드로 바뀐 탄속까지 반영
                            Fx.Spawn("fx_muzzle", player.transform.position + (Vector3)(d * 0.7f), 1f, c, 30f, angle + arm * 180f, 15);
                        }
                        angle += 22f;
                        fx.Play("gunshot", 0.35f, Random.Range(0.95f, 1.25f));
                        if (Mathf.Repeat(t, 0.2f) < 0.04f) fx.Shake(0.12f, 0.05f);
                        yield return new WaitForSeconds(0.04f);
                    }
                }
                break;

            case FlameId:
                PlayerMotion.Of(player).Cast(1f);
                // 화염 회오리: 마우스 위치에 불기둥이 생겨 적에게 다가가며 빨아들이고 태움
                {
                    GameObject go = new GameObject("FireTornado");
                    go.transform.position = MouseWorld();
                    FireTornado ft = go.AddComponent<FireTornado>();
                    ft.owner = this;
                    ft.damage = dmg * 0.25f;
                    ft.life = 4f + 1.5f * UltTrait(id);
                    fx.Play("ignite", 1f, 0.6f);
                    fx.Play("bigboom", 0.7f, 0.8f);
                    fx.Play("roar", 1f, 0.9f);
                    Fx.Spawn("fx_explosion", go.transform.position, 6f, Color.white, 14f);
                    Fx.Spawn("fx_shock", go.transform.position, 9f, new Color(1f, 0.5f, 0.15f), 16f);
                    fx.Shake(0.4f, 0.2f);
                }
                break;

            case SeekerId:
                PlayerMotion.Of(player).Cast(1.2f);
                // 영혼 떼: 영혼 구슬 다섯이 주위를 돌다 조준한 적에게 번갈아 달려듦 (4초)
                {
                    GameObject go = new GameObject("SoulSwarm");
                    SoulSwarm sw = go.AddComponent<SoulSwarm>();
                    sw.owner = player.transform;
                    sw.targets = alive;
                    sw.damage = dmg * 0.45f;
                    sw.count = 5 + UltTrait(id);
                    sw.color = c;
                    fx.Play("shimmer", 1f, 1.1f);
                    fx.Play("pulse", 0.8f, 1.3f);
                    Fx.Spawn("fx_soulburst", player.transform.position, 6f, Color.white, 16f);
                }
                break;

            case ChainId:
                // 뇌운: 2.5초 동안 하늘에서 번개가 조준한 적들에게 연달아 떨어짐
                // 1.3.3~ 번개는 하늘에서 저절로 떨어지므로 따로 돌리고 일제 사격은 바로 끝냄 → 뇌운 동안에도 총을 쏠 수 있음 (예전엔 2.5초 넘게 평타가 막혔음)
                PlayerMotion.Of(player).Cast(0.35f);
                StartCoroutine(ThunderCloud(alive, dmg, c, 2.5f + 0.8f * UltTrait(id)));
                break;

            case ScytheId:
                PlayerMotion.Of(player).Spin(1.2f, 3f);
                // 죽음의 춤: 무적 상태로 조준한 적들 사이를 순간이동하며 벤 뒤 제자리로 돌아옴
                // 카메라는 플레이어를 따라가지 않고 움직이는 범위의 가운데에 고정
                {
                    SpriteRenderer body = player.GetComponent<SpriteRenderer>();
                    if (alive.Count == 0)
                    {
                        Transform near = Specials.NearestEnemy(player.transform.position, 12f);
                        if (near != null) alive.Add(near.GetComponent<EnermyController>());
                        alive.RemoveAll(e => e == null);
                    }
                    player.GrantInvincibility(0.2f * alive.Count + 0.6f);
                    Vector3 home = player.transform.position;
                    Bounds span = new Bounds(home, Vector3.zero);
                    foreach (EnermyController t in alive) if (Alive(t)) span.Encapsulate(t.transform.position);
                    fx.HoldCamera(span.center);
                    foreach (EnermyController t in alive)
                    {
                        if (!Alive(t)) continue;
                        Vector3 from = player.transform.position;
                        Vector3 to = ClampToArena(t.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.2f));
                        // 잔상
                        if (body != null)
                        {
                            GameObject g = MakeSprite("DanceGhost", body.sprite, from, 1f, new Color(0.8f, 0.55f, 1f, 0.5f), "Character", -1);
                            g.transform.localScale = player.transform.lossyScale;
                            g.AddComponent<FadeOut>().duration = 0.3f;
                        }
                        Fx.Beam(from, to, 0.3f, new Color(0.8f, 0.55f, 1f, 0.7f), 0.15f);
                        player.transform.position = to;
                        player.FaceTowards(t.transform.position);
                        Fx.Spawn("fx_slash", t.transform.position, 5f, c, 30f, Random.Range(0f, 360f), 16);
                        using (var ovl = PhysHits.Circle(t.transform.position, 2.5f)) foreach (Collider2D col in ovl)
                            if (col.CompareTag("enermy") || col.CompareTag("boss"))
                                Specials.Damage(col.gameObject, dmg * 1.2f, (col.transform.position - to).normalized, 1.5f);
                        fx.Play("slash", 1f, Random.Range(0.9f, 1.15f));
                        Fx.Spawn("fx_slash", t.transform.position, 7f, Color.white, 34f, Random.Range(0f, 360f), 17);
                        Fx.Spawn("fx_soulburst", t.transform.position, 3f, Color.white, 22f);
                        fx.Shake(0.2f, 0.06f);
                        yield return new WaitForSeconds(0.12f);
                    }
                    // 제자리로 복귀
                    {
                        Vector3 from = player.transform.position;
                        if (body != null)
                        {
                            GameObject g = MakeSprite("DanceGhost", body.sprite, from, 1f, new Color(0.8f, 0.55f, 1f, 0.5f), "Character", -1);
                            g.transform.localScale = player.transform.lossyScale;
                            g.AddComponent<FadeOut>().duration = 0.3f;
                        }
                        Fx.Beam(from, home, 0.3f, new Color(0.8f, 0.55f, 1f, 0.7f), 0.15f);
                        player.transform.position = home;
                    }
                    Fx.Spawn("fx_soulburst", player.transform.position, 4f, Color.white, 18f);
                    yield return new WaitForSeconds(0.15f);
                    fx.ReleaseCamera();
                }
                break;

            case GrenadeId:
                PlayerMotion.Of(player).Throw(0.4f, AimDir());
                // 용암 융단폭격: 플레이어에서 마우스 쪽으로 줄지어 운석이 떨어짐
                {
                    Vector3 start = player.transform.position;
                    Vector2 dir = AimDir();
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    for (int row = 1; row <= GrenadeRows; row++)
                    {
                        for (int col = -1; col <= 1; col++)
                        {
                            Vector3 at = ClampToArena(start + (Vector3)(dir * row * 2.6f + side * col * 2.6f));
                            StartCoroutine(Bombard(at, dmg * 0.7f));
                        }
                        yield return new WaitForSeconds(0.12f);
                    }
                }
                break;

            default:
                yield break;
        }
    }

    IEnumerator Bombard(Vector3 at, float damage)
    {
        FxAnim rock = Fx.Play("fx_meteor", at + Vector3.up * 8f, 2.2f, Color.white, 16f, 0f, 20, true, 0.35f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (rock != null) rock.transform.position = Vector3.Lerp(at + Vector3.up * 8f, at, t / 0.3f);
            yield return null;
        }
        if (rock != null) Destroy(rock.gameObject);
        Explode(at, 2f, damage, 1.2f, new Color(1f, 0.45f, 0.1f, 0.9f));
        Fx.Spawn("fx_explosion", at, 5f, Color.white, 16f);
        if (Random.value < 0.4f) fx.Play("bigboom", 0.5f, Random.Range(0.9f, 1.2f));
    }

    // 지그재그 번개 선
    void DrawBolt(Vector3 a, Vector3 b, Color color, float duration)
    {
        GameObject go = new GameObject("Bolt");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.sortingLayerName = "Effect";
        lr.sortingOrder = 22;
        const int seg = 8;
        lr.positionCount = seg + 1;
        Vector3 side = Vector3.Cross(b - a, Vector3.forward).normalized;
        for (int i = 0; i <= seg; i++)
        {
            float k = i / (float)seg;
            float jitter = i == 0 || i == seg ? 0f : Random.Range(-0.6f, 0.6f);
            lr.SetPosition(i, Vector3.Lerp(a, b, k) + side * jitter);
        }
        lr.startColor = Color.white;
        lr.endColor = color;
        lr.startWidth = 0.3f;
        lr.endWidth = 0.15f;
        go.AddComponent<LineFade>().duration = duration;
        Fx.Bolt(a, b, 1.1f, color, duration + 0.05f);
        Fx.Spawn("fx_spark", b, 1.6f, color, 20f);
    }

    // 낫 모양 그림을 총알에 붙임 (판정은 총알 그대로)
    void AttachScytheVisual(Bullet b, float worldSize)
    {
        if (b.TryGetComponent(out SpriteRenderer bulletSr)) bulletSr.enabled = false;
        // 총알 프리팹은 가로 3.3 · 세로 -0.02 로 납작해서, 축마다 따로 되돌려야 제 모양이 나옴
        Vector3 ls = b.transform.lossyScale;
        float sx = Mathf.Abs(ls.x) < 0.0001f ? 1f : ls.x, sy = Mathf.Abs(ls.y) < 0.0001f ? 1f : ls.y;
        GameObject blade = MakeSprite("ScytheBlade", ScytheSprite, b.transform.position, 1f, Color.white, "Effect", 12);
        blade.transform.SetParent(b.transform, true);
        blade.transform.localRotation = Quaternion.identity;
        blade.transform.localScale = new Vector3(worldSize / sx, worldSize / sy, 1f);
        GameObject aura = MakeSprite("ScytheGlow", glowSprite, b.transform.position, 1f, new Color(0.7f, 0.4f, 1f, 0.5f), "Effect", 11);
        aura.transform.SetParent(b.transform, true);
        aura.transform.localRotation = Quaternion.identity;
        aura.transform.localScale = new Vector3(worldSize * 0.35f / sx, worldSize * 0.35f / sy, 1f);
    }

    // ================================================================= 필살기 강화 (영혼 트리) · 조준 화면
    public const int PistolUlt = -1;
    public const int UltPowerStat = 0, UltTraitStat = 1;
    public static readonly int[] UltStatMax = { 5, 3 };
    readonly Dictionary<int, int[]> ultLevels = new Dictionary<int, int[]>();

    public int UltLevel(int weapon, int stat) => ultLevels.TryGetValue(weapon, out int[] l) ? l[stat] : 0;
    // 필살기 위력 (특성 강화 단계 + 영혼 트리)
    public float UltPower(int weapon) => 1f + 0.2f * UltLevel(weapon, UltPowerStat) + TreeUltPower;
    public int UltTrait(int weapon) => UltLevel(weapon, UltTraitStat);

    public bool UpgradeUlt(int weapon, int stat)
    {
        if (UltLevel(weapon, stat) >= UltStatMax[stat]) return false;
        if (!ultLevels.ContainsKey(weapon)) ultLevels[weapon] = new int[2];
        ultLevels[weapon][stat]++;
        if (fx != null) { fx.Play("chime", 0.5f, 1.2f); fx.Play("pulse", 0.4f, 1.5f); }
        return true;
    }

    public static string UltName(int weapon) => weapon == PistolUlt ? Loc.T(CharacterData.IsGunner ? "일제 사격" : CharacterData.Current.skill)
                                                  : IsKit(weapon) ? Loc.T(KitUltName(weapon)) : VolleyName(weapon);

    public static string UltTraitName(int weapon) => weapon switch
    {
        PistolUlt when !CharacterData.IsGunner => Loc.T("효과 범위"),
        _ when IsKit(weapon) => Loc.T("효과 범위"),
        PistolUlt => Loc.T("타겟 수"), ScytheId => Loc.T("타겟 수"), SeekerId => Loc.T("영혼 구슬"), ShotgunId => Loc.T("포격 횟수"),
        SniperId => Loc.T("광선 굵기"), DualId => Loc.T("지속 시간"), FlameId => Loc.T("회오리 지속"), ChainId => Loc.T("뇌운 지속"), GrenadeId => Loc.T("폭격 줄"),
        _ => Loc.T("특성"),
    };

    public static string UltTraitStep(int weapon) => weapon switch
    {
        PistolUlt when !CharacterData.IsGunner => "+10%",
        _ when IsKit(weapon) => "+10%",
        PistolUlt => Loc.T("+2마리"), ScytheId => Loc.T("+2마리"), SeekerId => Loc.T("+1개"), ShotgunId => Loc.T("+1회"), SniperId => "+30%",
        DualId => Loc.T("+0.4초"), FlameId => Loc.T("+1.5초"), ChainId => Loc.T("+0.8초"), GrenadeId => Loc.T("+2줄"),
        _ => "",
    };

    // 조준할 수 있는 적 수: 권총 · 낫은 강화로 늘어남
    public int MaxTargets(int baseCount)
    {
        int w = UltReplaced ? UltId : PistolUlt;
        return w == PistolUlt || w == ScytheId ? baseCount + 2 * UltTrait(w) : baseCount;
    }

    public int GrenadeRows => 6 + 2 * UltTrait(GrenadeId);
    // 총알 폭풍 사거리 (칸): 기본 18 (6은 너무 짧았음), 영혼 트리 「폭풍 사거리」 I · II 로 +3씩
    public const float StormBaseRange = 18f;
    public float StormRange => StormBaseRange + TreeStormRange;

    // 조준이 필요 없는 필살기 (우클릭 즉시 발동)
    // 화염 방사기 · 저격총은 우클릭을 누른 채 자리를 조준 (회오리 위치 · 레일건 방향)
    // 2.2.0: 거너는 쌍권총(총알 폭풍)만 필살기를 바꾸고, 나머지 진화 무기는 원래 조준 사격 뒤에 궁극기가 절반 세기로 덧붙음 (다른 캐릭터와 같게)
    // 1.0.1: 쌍권총도 필살기를 바꾸지 않음 — 일제 사격을 그대로 쓰고 총알 폭풍이 절반 세기로 덧붙음 (모든 무기 진화가 같은 규칙)
    public bool UltReplaced => false;
    public bool UltAdds => UltActive && !UltReplaced;
    public bool IsInstantUlt => UltReplaced;

    // 조준 화면 (WeaponAim)이 쓰는 값들
    public Transform PlayerTransform => player.transform;
    public Vector3 MouseWorldPos => MouseWorld();
    public Vector2 AimDirection => AimDir();
    public Vector3 PlayerMuzzle => player.MuzzlePosition;
    public static Color ColorOf(int id) => WeaponColor(id);
    // 타겟팅 중 화면 색 (무기 색)
    public Color AimTint => UltReplaced ? WeaponColor(UltId) : new Color(1f, 0.9f, 0.6f);

    WeaponAim aim;
    // 1.0.1: 쌍권총이 아닌 진화도 덧붙는 필살기(지옥불 포격 부채꼴 · 레일건 직선 …)의 범위를 조준 중에 보여 줌 (표식은 그대로 일제 사격 것)
    bool AimShown => Gunner && UltActive;
    // 1.0.2: 1차 · 2차 진화가 다르면 두 궁극기가 모두 덧붙으므로 1차 것의 범위도 따로 그림 (aim2)
    WeaponAim aim2;
    int FirstAddedUlt { get { List<int> ids = AddedUltIds(); return ids.Count > 1 ? ids[0] : -1; } }
    public void BeginAim()
    {
        if (!AimShown) return;
        if (aim == null) aim = new WeaponAim(this);
        aim.Begin(UltId);
        int first = FirstAddedUlt;
        if (first >= 0) { if (aim2 == null) aim2 = new WeaponAim(this); aim2.Begin(first); }
        else if (aim2 != null) aim2.End();
    }
    public void UpdateAim(List<EnermyController> targets, float charge)
    {
        if (!AimShown) return;
        if (aim != null) aim.Update(targets, charge);
        if (aim2 != null) aim2.Update(targets, charge);
    }
    public void EndAim() { if (aim != null) aim.End(); if (aim2 != null) aim2.End(); }
    public GameObject MarkTarget(Transform target) => aim != null && UltReplaced ? aim.MarkTarget(target) : null;

    // ================================================================= evolution
    // order: 한 번에 여러 개를 진화할 때 알림 글자를 위로 쌓는 순서
    // 능력이 진화했을 때 (업적 등)
    public static event System.Action<int> Evolved;

    public void Evolve(int id, int order = 0)
    {
        if (!equipped.Contains(id) || evolved.Contains(id)) return;
        evolved.Add(id);
        Evolved?.Invoke(id);
        if (id == OrbsId) SpawnOrbs(2);
        KitOnEquip(id, true);

        if (fx != null)
        {
            fx.Play("pulse", 0.9f, 0.9f);
            fx.Play("chime", 0.8f, 1.1f);
            if (player != null) fx.FloatText(player.transform.position + Vector3.up * (1.4f * order), Loc.T(abilities[id].name) + Loc.T(" 진화!"), new Color(1f, 0.85f, 0.4f), 6f, 0f);
        }
        if (player != null) Flash(player.transform.position, 7f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.6f);
    }

    // 능력 설명 본문 (번역됨, "진화:" 줄은 뺌)
    public static string BodyText(int id, string description)
    {
        string desc = Loc.T(description);
        if (!IsKit(id)) return desc;
        int cut = desc.LastIndexOf('\n');
        return cut >= 0 ? desc.Substring(0, cut) : desc;
    }

    // 모든 특수 능력 설명의 모양: 본문 + 파란 "진화" 줄
    public static string AbilityText(int id, string description)
    {
        string evo = EvolveText(id);
        return BodyText(id, description) + (evo.Length > 0 ? "\n<color=#9fd8ff>" + Loc.T("진화") + "</color>  " + evo : "");
    }

    // 진화 효과 설명 (번역된 문장). 거너 능력은 EvolveTexts, 캐릭터 능력(20~)은 설명의 마지막 "진화:" 줄
    public static string EvolveText(int id)
    {
        if (id >= 0 && id < EvolveTexts.Length) return Loc.T(EvolveTexts[id]);
        if (!IsKit(id)) return "";
        string desc = Loc.T(KitDesc(id));
        string last = desc.Substring(desc.LastIndexOf('\n') + 1);
        int colon = last.IndexOfAny(new[] { ':', '：' });
        return colon >= 0 ? last.Substring(colon + 1).Trim() : last;
    }

    // 진화 효과 설명 (ID 순서, 한국어 원문 · 쓸 때 Loc.T로 번역)
    public static readonly string[] EvolveTexts =
    {
        "사거리 +2, 부채꼴이 넓어지고 피해 320%. 맞은 적이 불탑니다.",
        "충전 시간 0.8초. 완충 사격이 맞은 곳에서 폭발합니다.",
        "두 총구에서 동시에 발사합니다 (탄약 소모는 그대로).",
        "열이 40% 덜 오르고 불길 화상 피해가 증가합니다.",
        "한 번에 유도탄 2발을 쏩니다.",
        "번개가 7번 튀고, 튈 때 피해가 덜 줄어듭니다.",
        "낫이 더 커지고 더 멀리 날아가며 피해 220%.",
        "폭발 후 작은 용암탄 3개로 흩어집니다.",
        "쿨타임 1.8초. 도착 지점에서 충격파가 터집니다.",
        "장판이 더 넓고 6초 동안 지속됩니다. 쿨타임 9초.",
        "적이 75% 느려지고 7초 동안 지속됩니다.",
        "체력을 잃지 않고 12초 동안 지속됩니다.",
        "영혼 3개부터 쓸 수 있고 폭발 범위가 넓어집니다.",
        "해골 5마리를 부르고 쿨타임 10초.",
        "분신이 5초 동안 남고 사라질 때 폭발합니다.",
        "쿨타임 2초, 갈고리 피해 300%.",
        "영혼이 5개로 늘고 피해가 증가합니다.",
        "반격 범위와 피해가 크게 늘어납니다.",
        "탄창의 마지막 두 발이 저주탄이 됩니다.",
        "두 번 부활하고, 부활할 때 체력 50%로 일어납니다.",
    };

    // ================================================================= weapon upgrades (shop)
    public int WeaponLevel(int id, int stat) => weaponLevels.TryGetValue(id, out int[] l) ? l[stat] : 0;

    public bool UpgradeWeapon(int id, int stat)
    {
        if (!weapons.Contains(id) || WeaponLevel(id, stat) >= WeaponStatMax[stat]) return false;
        if (!weaponLevels.ContainsKey(id)) weaponLevels[id] = new int[4];
        int oldMag = MagSize(id);
        weaponLevels[id][stat]++;
        // 탄창이 커지면 늘어난 만큼 바로 채움
        if (stat == StatMag && UsesAmmo(id)) Ammo(id).ammo += MagSize(id) - oldMag;
        if (fx != null)
        {
            fx.Play("clank", 0.6f, 1.2f);
            fx.Play("chime", 0.4f, 1.4f);
        }
        return true;
    }

    float WeaponDamageMul(int id) => 1f + 0.15f * WeaponLevel(id, StatDamage);
    float WeaponRateMul(int id) => Mathf.Pow(0.9f, WeaponLevel(id, StatRate));
    int Trait(int id) => WeaponLevel(id, StatTrait);

    // 무기별 특성 강화 이름과 한 단계 효과
    public static string TraitName(int id) => id switch
    {
        ShotgunId => Loc.T("사거리"),
        SniperId => Loc.T("충전 속도"),
        DualId => Loc.T("관통"),
        FlameId => Loc.T("냉각"),
        SeekerId => Loc.T("관통"),
        ChainId => Loc.T("연쇄"),
        ScytheId => Loc.T("낫 크기"),
        GrenadeId => Loc.T("폭발 범위"),
        _ => Loc.T("특성"),
    };

    public static string TraitStep(int id) => id switch
    {
        ShotgunId => Loc.T("사거리 +1"),
        SniperId => Loc.T("충전 시간 -15%"),
        DualId => Loc.T("관통 +1"),
        FlameId => Loc.T("열 발생 -20%"),
        SeekerId => Loc.T("관통 +1"),
        ChainId => Loc.T("연쇄 +1"),
        ScytheId => Loc.T("크기·거리 +15%"),
        GrenadeId => Loc.T("범위 +0.6"),
        _ => "",
    };

    // ================================================================= update
    void Update()
    {
        DamageSource.Current = DamageSource.Special;     // 오라 · 패시브 · 캐릭터 능력 (무기 · 스킬은 아래에서 그 능력 이름)
        try { UpdateAbilities(); }
        finally { DamageSource.Current = null; }
    }

    // 코루틴 · 소환물도 시작할 때의 피해 출처를 이어 감 (RunStats)
    public new Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(DamageSource.Keep(routine));

    // 이번 판 피해 기록에 쓰는 능력 이름 (번역 전 한국어)
    string SourceOf(int id) => abilities != null && id >= 0 && id < abilities.Length && abilities[id] != null ? abilities[id].name : DamageSource.Special;

    void UpdateAbilities()
    {
        if (player != null) TreeTick();
        if (player == null || equipped.Count == 0 || Time.timeScale == 0f || PlayerController.Cinematic)
        {
            // 멈춘 동안에는 소리와 표시를 숨기고 조준 중이던 스킬은 취소 (멈춘 사이 키를 떼도 조준이 남지 않게)
            aimingSkill = -1;
            if (fx != null) fx.StopLoop();
            HidePreviews();
            UpdateHud();
            return;
        }

        HidePreviews();

        if (weapons.Count > 0)
        {
            // 무기 진화 방식(거너)은 진화한 무기를 늘 들고 있음 (교체 없음)
            if (!UsesEvolution && KeyBindings.Down(GameAction.Swap))
            {
                // 기본 권총(-1) → 무기1 → 무기2 → 기본 권총 ...
                weaponIndex = weaponIndex + 1 >= weapons.Count ? -1 : weaponIndex + 1;
                CancelSniperCharge();
                if (flameMuzzle != null) Destroy(flameMuzzle);
                flameWasFiring = false;
                fx.StopLoop();
                fx.Play("clank", 0.5f, 1.4f);
                fx.FloatText(player.transform.position, WeaponActive ? Loc.T(abilities[CurrentWeapon].name) : Loc.T(CharacterData.IsGunner ? "기본 권총" : CharacterData.Current.weapon), new Color(0.96f, 0.83f, 0.47f), 4.5f, 0f);
            }
            UpdateWeaponReloads();
            // 무기를 들고 있으면 그 무기의 탄창을 표시 (R: 들고 있는 무기 장전)
            player.ammoTextOverride = WeaponActive ? (AmmoText(CurrentWeapon) ?? (IsKit(CurrentWeapon) ? Loc.T(abilities[CurrentWeapon].name) : null)) : CharacterKit.Instance != null ? CharacterKit.Instance.WeaponName : null;
            if (WeaponActive && KeyBindings.Down(GameAction.Reload)) StartWeaponReload(CurrentWeapon);
            if (WeaponActive) using (DamageSource.As(SourceOf(CurrentWeapon))) UpdateWeapon();
        }

        for (int i = 0; i < skills.Count && i < SkillKeys.Length; i++) HandleSkillKey(skills[i], SkillKeys[i]);
        HandleRelicKey();                                   // 1.2~ [Q] 유물 칸 (SpecialAbilities.Relics)
        if (aimingSkill >= 0) ShowSkillPreview(aimingSkill);

        if (Has(OrbsId)) using (DamageSource.As(SourceOf(OrbsId))) UpdateOrbs();
        KitTick();
        if (CurrentWeapon != FlameId) heat = Mathf.Max(0f, heat - Time.deltaTime * 0.35f);

        UpdateReadyChimes();
        UpdateCurseNotice();
        UpdateAuras();
        UpdateHud();
    }

    Vector3 MouseWorld()
    {
        Vector3 m = player.MainCamera.ScreenToWorldPoint(GameInput.MousePosition);
        m.z = 0f;
        return m;
    }

    // 특수 능력은 캐릭터 공격력 배율(검사 3배 · 도적 0.7배 …)을 빼고 계산 (캐릭터끼리 특수 능력 세기가 같게)
    float Damage => player.damage * player.damageMultiplier / Mathf.Max(0.1f, CharacterData.Current.damage);
    // 들고 있는 무기의 강화가 반영된 피해
    float WDamage => Damage * WeaponDamageMul(CurrentWeapon) * InertBonus;      // 안 맞게 된 거너 카드 → 무기 피해 (SpecialAbilities.Fit)
    bool OverUI => !GameInput.Auto && !GamePad.Aiming && UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
}

// 할당 없는 원 범위 검색 (Physics2D.OverlapCircleAll 과 같은 결과 · 같은 순서 — 기본 레이캐스트 층 · 트리거 여부는 물리 설정 그대로)
// 부를 때마다 빌려 둔 목록 하나를 쓰고 using 이 끝나면 돌려놓음 → 피해 콜백 안에서 또 검색해도(처치 → 폭발 → 검색) 서로 다른 목록이라 안전
//   using (var ovl = PhysHits.Circle(pos, r)) foreach (Collider2D c in ovl) { … }
public readonly struct PhysHits : System.IDisposable
{
    static readonly Stack<List<Collider2D>> pool = new Stack<List<Collider2D>>();
    static ContactFilter2D filter;
    public readonly List<Collider2D> List;

    PhysHits(List<Collider2D> list) { List = list; }

    // allLayers: Specials.Overlap 과 같은 조건 (층 거르기 없음)
    public static PhysHits Circle(Vector2 pos, float radius, bool allLayers = false)
    {
        List<Collider2D> l = pool.Count > 0 ? pool.Pop() : new List<Collider2D>(64);
        l.Clear();
        filter.useTriggers = Physics2D.queriesHitTriggers;
        filter.useLayerMask = !allLayers;
        filter.layerMask = Physics2D.DefaultRaycastLayers;
        Physics2D.OverlapCircle(pos, radius, filter, l);
        return new PhysHits(l);
    }

    public int Count => List.Count;
    public Collider2D this[int i] => List[i];
    public List<Collider2D>.Enumerator GetEnumerator() => List.GetEnumerator();

    public void Dispose()
    {
        if (List == null) return;
        List.Clear();
        if (pool.Count < 32) pool.Push(List);
    }
}
