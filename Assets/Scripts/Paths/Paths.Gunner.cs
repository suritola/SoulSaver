using System.Collections.Generic;
using UnityEngine;

// 거너 갈래 5개 (기획서 3-1) — 데이터 · 훅 (효과 본체는 Paths.Gunner.Effects.cs · 포탑은 Paths.Gunner.Turret.cs)
// 거너 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Gunner.cs · 거너 기존 파일(SignatureSkills.Gunner.cs · SpecialAbilities.GunCards.cs ·
//   PlayerController.cs 의 거너 사격 부분) — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// PlayerController 와 잇는 곳 (모두 GunnerPaths.Live 가 null 이면 예전 그대로):
//   Shoot      : IntervalMul(연사) · NoAmmo(과열 사격) · Spread(난사) · BeginShot(피해 배율 · 저격탄 판정) · FireOverride(산탄 · 슬러그) · Dress(탄 꾸밈) · EndShot(열)
//   Reload     : ReloadTime(과열 시간) · ReloadRate(산탄 · 포탑 곁) · ForceReload(과열 → 장전 코루틴으로 사격 불가 시간)
//   일제 사격  : UltShot (관통 저격 극의)
//   SignatureSkills.Gunner : ReloadStarted(돌진 · 과열 폭발) · ReloadEnded(포탑) · TurretVolley(총잡이의 춤)
// 상태는 모두 이 인스턴스 필드 (판마다 새로) · 시간 상태는 「끝나는 시각」 값으로 (코루틴 켜고 끄기 없음)
public sealed partial class GunnerPaths : HeroPaths
{
    public const int Snipe = 0, Ricochet = 1, Overheat = 2, Shotgun = 3, Turret = 4;
    // 새 교차 번호 (Crosses 배열 순서)
    public const int CrossApInc = 2, CrossFlameDash = 3;

    public override CharacterId Who => CharacterId.Gunner;
    public override string IconKey => "gunner";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "g.snipe", name = "관통 저격", tagline = "멈춰서 모아 쏘는 관통 저격탄",
            seed = "저격 자세", seedDesc = "멈춰 있으면 0.6초마다 조준이 모여 다음 탄이 저격탄이 됩니다. (모든 적 관통 · 피해 ×2.2 · 탄속 ×1.6)",
            cost = "움직이며 쏜 탄의 피해가 25% 줄고, 탄창이 8발에서 6발로 줄어듭니다.", strong = "보스 · 일렬", weak = "기동 · 포위",
            cards = new[] { G, S(3), S(9) }, evo = "ge.seeker",
            res1 = "저격탄이 약점에 맞으면 피해 ×1.5와 함께 작은 폭발이 일어납니다.",
            res2 = "저격탄이 꿰뚫은 적 수만큼 다음 조준이 빨라집니다. (5마리면 즉시)",
            mastery = "일제 사격(우클릭)이 모두 저격탄이 됩니다.",
            color = new Color(0.55f, 0.85f, 1f), form = "영혼 저격총" },
        new PathDef {
            key = "g.ricochet", name = "도탄 난사", tagline = "큰 탄창 · 튕기는 난사",
            seed = "난사", seedDesc = "탄창이 14발로 늘고 연사가 1.35배 빨라지며, 모든 탄이 한 번 튕깁니다. (퍼짐 ±8°)",
            cost = "한 발의 피해가 30% 줄어듭니다.", strong = "무리", weak = "보스 단일 · 탄 관리",
            cards = new[] { P, S(1), A, S(8) }, evo = "ge.storm",
            res1 = "튕길 때마다 다음 튕김의 피해가 15% 늘어납니다. (최대 3회)",
            res2 = "투기장 경계에 닿은 탄도 되튕기고, 사라질 때 파편 두 개가 튑니다.",
            mastery = "튕김 횟수가 무제한이 됩니다. (튕길 때마다 피해 −10%)",
            color = new Color(1f, 0.85f, 0.35f), form = "쌍권총" },
        new PathDef {
            key = "g.overheat", name = "화염 과열", tagline = "장전 대신 열 게이지 · 과열 폭발",
            seed = "과열 사격", seedDesc = "장전 대신 열 게이지가 찹니다. 열 50% 위에서는 모든 탄이 화상을 입히고, 100%가 되면 2초 동안 과열되며 몸 둘레에 화염 폭발이 일어납니다. (공격력 250%)",
            cost = "과열된 2초 동안 쏠 수 없고, 직접 피해가 15% 줄어듭니다.", strong = "지속 광역 · 보스 화상", weak = "과열 순간",
            cards = new[] { M, K, S(2) }, evo = "ge.furnace",
            res1 = "화상이 3중첩까지 쌓입니다.",
            res2 = "과열 폭발 자리에 4초 동안 불 장판이 남고, 열 90~100%에서 쏜 탄은 피해 ×1.5입니다.",
            mastery = "과열 시간이 0.8초로 줄고 폭발이 두 번 일어납니다.",
            color = new Color(1f, 0.5f, 0.2f), form = "화염 방사기" },
        new PathDef {
            key = "g.shotgun", name = "산탄 돌격", tagline = "탄창 4 산탄 · 장전 돌진",
            seed = "산탄 장전", seedDesc = "평타가 6발 산탄이 됩니다. (사거리 6칸 · 40° · 가까울수록 셈) 탄창은 4발, 장전은 40% 빠르고, 장전을 시작하면 이동 방향으로 2.5칸 돌진합니다. (0.15초 무적)",
            cost = "6칸 밖의 적은 거의 맞히지 못합니다.", strong = "근접 무리 · 생존", weak = "원거리 · 탄막 보스",
            cards = new[] { B, S(4), S(7) }, evo = "ge.thunder",
            res1 = "돌진에 부딪힌 적을 밀치고 다음 산탄의 피해가 40% 늘어납니다.",
            res2 = "탄창의 마지막 발이 일직선으로 꿰뚫는 슬러그가 됩니다. (공격력 300%)",
            mastery = "돌진을 두 번 연달아 하고, 끝에 충격파가 터집니다.",
            color = new Color(1f, 0.4f, 0.35f), form = "산탄총" },
        new PathDef {
            key = "g.turret", name = "포탑 설치", tagline = "장전할 때 포탑 · 진지 방어",
            seed = "거치 포탑", seedDesc = "장전을 마칠 때마다 발밑에 포탑이 서서 0.4초마다 가까운 적을 쏩니다. 사거리 안에 보스가 있으면 보스를 먼저 쏩니다. (공격력 20% · 최대 2개 · 12초)",     // 1.4.2~ 보스 우선 (갈래 강점 「서 있는 보스」대로)
            cost = "내 공격 속도가 20% 느려집니다.", strong = "진지 방어 · 서 있는 보스", weak = "이동 강제 (침식 · 결계)",
            cards = new[] { S(0), S(5), S(6) }, evo = "ge.dance",
            res1 = "포탑이 3개까지 서고, 포탑 4칸 안에 서 있으면 장전이 2배 빨라집니다.",
            res2 = "포탑이 0.8초마다 적 투사체 하나를 쏴서 지우고, 사라질 때 폭발합니다.",
            mastery = "포탑이 사라지지 않고 4개까지 서며, 탄피 지뢰를 깝니다.",
            color = new Color(0.5f, 0.95f, 0.5f), form = "용암 유탄" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Ricochet, Turret, "ge.thunderback"),          // 0 뇌격 반격 (A · S5)
        Cross(Ricochet, Snipe, "ge.outlaw"),                // 1 무법자 (S8 · S9)
        Cross(Snipe, Overheat, "g.x.apinc", "철갑 소이탄", "저격탄이 지나간 줄 전체에 화상이 3중첩으로 붙습니다."),          // 2 (CrossApInc)
        Cross(Overheat, Shotgun, "g.x.flamedash", "화염 돌진", "과열 폭발이 돌진 끝에서 터지고, 과열 중에도 돌진할 수 있습니다."),   // 3 (CrossFlameDash)
    };

    // ================================================================= 수치 (기획서 3-1 · 4장 — 갈래 효과 피해는 모두 Paths.Power(갈래)를 곱함)
    // 관통 저격
    const float AimTime = 0.6f;                 // 멈춰 있으면 이 시간마다 조준이 모임
    const int BaseMag = 8;                      // 거너 기본 탄창 (GameScene 의 PlayerController.MaxBullet)
    const float SnipeMul = 2.2f, SnipeSpeed = 1.6f, MovingMul = 0.75f, SnipeMag = 0.75f;      // 탄창 8 → 6
    const float WeakMul = 1.5f, WeakBoom = 0.6f;                 // 공명 I: 약점 ×1.5 · 작은 폭발 (공격력 60%)
    const float PierceAim = 0.2f;                                // 공명 II: 꿰뚫은 적 하나마다 조준 20% (5마리면 즉시)
    // 도탄 난사
    const float RicochetMag = 1.75f, RicochetRate = 1.35f, RicochetSpread = 8f, RicochetCost = 0.7f;       // 탄창 8 → 14
    const float BounceBase = 0.5f, BounceCard = 0.1f;            // 튕긴 탄 피해: 맞힌 탄의 50% (+ 「도탄 사격」 단계마다 10%)
    const float BounceGrow = 1.15f, BounceDecay = 0.9f;          // 공명 I (+15%, 3회까지) · 극의 (튕길 때마다 −10%)
    const int BounceGenMax = 12, BounceLiveMax = 300;            // 「무제한」 세대 상한 · 동시에 날아가는 튕긴 탄 상한 (기획서 6-3)
    const float FragRate = 0.3f;                                 // 공명 II 파편 (공격력 30%)
    // 화염 과열
    const float HeatPerShot = 1f / 12f, CoolDelay = 0.6f, CoolRate = 0.35f;   // 열: 한 발 1/12 · 0.6초 쉬면 초당 35% 식음
    const float OverheatTime = 2f, OverheatTimeMastery = 0.8f, OverheatCost = 0.85f;
    const float BoomRate = 2.5f, BoomRadius = 4f, HotMul = 1.5f;
    const float BurnRate = 0.2f, BurnTime = 2.5f;                 // 화상 한 겹: 초당 공격력 20% · 2.5초
    const float FireFloorRate = 0.2f, FireFloorTime = 4f, FireFloorRadius = 3f;
    // 산탄 돌격
    const int Pellets = 6;
    const float PelletRate = 0.22f, PelletArc = 40f, PelletRange = 6f, ShotgunMag = 0.5f, ShotgunReload = 1f / 0.6f;   // 탄창 8 → 4 · 장전 40% 빠름
    const float DashDist = 2.5f, DashTime = 0.14f, DashInvuln = 0.15f;
    const float RamRate = 0.3f, RamBoost = 1.4f, SlugRate = 3f, SlugRange = 14f, QuakeRate = 1.5f;
    // 포탑 설치
    internal const float TurretRate = 0.2f, TurretEvery = 0.4f, TurretRange = 9f, TurretLife = 12f, TurretSlow = 1.25f;
    internal const float TurretZapEvery = 0.8f, TurretZapRange = 6f, TurretBoom = 1f, TurretMineEvery = 5f, DanceRate = 0.6f;

    static readonly Color SnipeColor = new Color(0.55f, 0.9f, 1f);
    static readonly Color BounceColor = new Color(1f, 0.9f, 0.4f);
    static readonly Color FireColor = new Color(1f, 0.55f, 0.2f);
    static Color SlugColor => VisualRules.Kit(new Color(1f, 0.45f, 0.3f), new Color(1f, 0.65f, 0.3f));     // 2단계 키트 색: 주홍 → 거너 금 쪽 30° (튕김탄 금색과 구분)
    internal static readonly Color TurretColor = new Color(0.55f, 1f, 0.55f);

    // 이번 판이 거너 갈래 규칙이면 효과 인스턴스 (아니면 null → 부르는 쪽은 예전 그대로)
    public static GunnerPaths Live => Paths.Active ? Paths.Hero as GunnerPaths : null;
    // 도탄 난사가 「도탄 사격」 카드의 튕김을 맡음 (SpecialAbilities.GunCards 가 카드 튕김을 건너뜀)
    public static bool OwnsRicochet => Live != null && Paths.Has(Ricochet);
    // 1.4.1~ 이 탄에 갈래 튕김(AttachRicochet)이 붙었나 — 붙은 탄만 카드 튕김을 건너뜀 (탄은 풀에서 다시 쓰므로 generation 까지 비교)
    public static bool OwnsRicochetOf(Bullet b) => OwnsRicochet && b != null && b.TryGetComponent(out GunPathBounce m) && m.gen == b.generation;

    // ================================================================= 판 흐름 훅
    public override void OnSeed(int path, bool replay)
    {
        // 탄창 크기: 기본 탄창(8발)만 바꾸고 그 차이를 더함 (저격 8 → 6 · 난사 8 → 14 · 산탄 8 → 4)
        //   산 탄창 · 진화로 늘어난 탄창은 그대로 더해지므로 고르는 순서와 결과가 같음
        //   판마다 새 플레이어 · 이어하기도 입문을 한 번만 다시 적용하므로 두 번 더해지지 않음
        float m = path == Snipe ? SnipeMag : path == Ricochet ? RicochetMag : path == Shotgun ? ShotgunMag : 1f;
        PlayerController p = Hostile.Player;
        if (p == null || m == 1f) return;
        int delta = Mathf.RoundToInt(BaseMag * m) - BaseMag;
        p.MaxBullet = Mathf.Max(2, p.MaxBullet + delta);
        // 입문 순간 공짜로 가득 채우지 않음 (이어하기 재적용은 판 시작이라 가득)
        if (replay && !p.IsReloading) p.NowBullet = p.MaxBullet;
        else p.NowBullet = Mathf.Clamp(p.NowBullet, 0, p.MaxBullet);
        // 상점이 들고 있는 탄창 값도 맞춤 (이어하기는 상점 창을 열지 않고 구매를 다시 적용해 옛 값 + 1 로 덮어쓰던 것)
        Shop coinShop = Object.FindFirstObjectByType<Shop>();
        if (coinShop != null) coinShop.MaxBullet = p.MaxBullet;
    }

    public override void End()
    {
        // 판이 끝남: 남은 포탑을 치움 (장판 · 탄은 스스로 사라짐)
        foreach (GameObject t in turrets) if (t != null) Object.Destroy(t);
        turrets.Clear();
        bounces.Clear();
        burns.Clear();
        dashing = false;
        tapFrame = -1;
    }

    // ================================================================= 전투 훅
    // 저격탄 · 화상탄이 맞힌 그 한 대 (GunHitTap 이 맞기 직전에 알려 줌): 약점 ×1.5 (관통 저격 공명 I)
    public override float Outgoing(GameObject target, float dmg)
    {
        if (tapFrame != Time.frameCount || tapInfo == null || !tapInfo.snipe || target == null) return 1f;
        if (Paths.Res(Snipe) < 1 || IdOf(target) != tapId) return 1f;
        if (DamagePopup.NextWeakTarget != target.transform) return 1f;
        tapWeak = true;
        return 1f + (WeakMul - 1f) * Paths.Power(Snipe);
    }

    public override void Hit(GameObject target, float dmg, bool killed, bool proc)
    {
        if (tapFrame != Time.frameCount || tapInfo == null || target == null || IdOf(target) != tapId) return;
        GunHitTap info = tapInfo;
        bool weak = tapWeak;
        tapInfo = null;                 // 아래 폭발 · 화상이 다시 이 한 대로 잡히지 않게 먼저 비움
        tapWeak = false;
        tapFrame = -1;
        OnTapHit(info, target, weak);
    }

    public override void Tick()
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying)
        {
            if (dashing) CancelDash();      // 돌진 중 쓰러짐: 부활 뒤 옛 목표로 순간이동 · 끝 효과가 뒤늦게 터지지 않게
            return;
        }
        float now = Time.time;
        if (Paths.Has(Snipe)) SnipeTick(p);
        if (Paths.Has(Overheat)) HeatTick(p, now);
        if (dashing) DashTick(p, now);
        if (boom2At > 0f && now >= boom2At) { boom2At = 0f; OverheatBoom(p.transform.position, true); }
        if (now >= pruneAt) { pruneAt = now + 3f; PruneBurns(now); }
    }

    // HUD 입문 칸 오른쪽 위 (값이 바뀔 때도 미리 만든 글만 돌려줌)
    static readonly string[] PctText = MakePct();
    static readonly string[] CountText = { null, "x1", "x2", "x3", "x4", "x5", "x6" };

    static string[] MakePct()
    {
        string[] s = new string[21];
        for (int i = 0; i <= 20; i++) s[i] = (i * 5) + "%";
        return s;
    }

    static string Pct(float v) => PctText[Mathf.Clamp(Mathf.RoundToInt(v * 20f), 0, 20)];

    public override string Badge(int path)
    {
        PlayerController p = Hostile.Player;
        switch (path)
        {
            case Snipe: return aim >= 1f ? Loc.T("저격") : aim > 0.05f ? Pct(aim) : null;
            case Overheat:
                if (p != null && p.IsReloading) return Loc.T("과열!");
                return heat > 0.01f ? Pct(heat) : null;
            case Turret:
                int n = 0;
                for (int i = 0; i < turrets.Count; i++) if (turrets[i] != null) n++;
                return n > 0 ? CountText[Mathf.Min(n, CountText.Length - 1)] : null;
        }
        return null;
    }

    // ================================================================= 맞히는 순간 알림 (GunHitTap → 여기)
    //   Bullet 은 맞힐 때 hitOnce.Add(대상 번호) → TakeDamage(Outgoing · Hit 훅) 순서로 부름. 우리 탄의 hitOnce 비교기가 Add 순간 번호를 알려 줌
    //   → 같은 프레임 · 같은 대상의 Outgoing · Hit 이 「우리 탄의 그 한 대」 (보스도 같음 · onHitEnemy 는 일반 적만 불려서 이 길을 씀)
    int tapId, tapFrame = -1;
    GunHitTap tapInfo;
    bool tapWeak;

    internal void Tapped(GunHitTap info, int id)
    {
        tapInfo = info;
        tapId = id;
        tapFrame = Time.frameCount;
        tapWeak = false;
    }

    // Bullet 이 hitOnce 에 넣는 번호 = 적은 EnermyController · 보스는 bosss 의 GetInstanceID
    static int IdOf(GameObject g)
    {
        if (g.TryGetComponent(out EnermyController e)) return e.GetInstanceID();
        if (g.TryGetComponent(out bosss b)) return b.GetInstanceID();
        return 0;
    }

    // 탄에 맞힘 알림을 붙임 (저격탄 · 화상탄만)
    //   원래 hitOnce 가 있던 탄: 같은 적은 한 번만 (원래대로) · 원래 null 이던 탄: 알림만 하고 같은 적 재명중도 원래대로 허용 (passThrough)
    void Tap(Bullet b, bool snipe, bool burn)
    {
        if (b == null) return;
        GunHitTap tap = new GunHitTap(this, snipe, burn, b.hitOnce == null);
        HashSet<int> set = new HashSet<int>(tap);
        if (b.hitOnce != null) foreach (int id in b.hitOnce) set.Add(id);      // 이미 넣어 둔 것은 그대로 (넣는 동안은 알림 안 함 · tap.set 이 아직 없음)
        tap.set = set;
        b.hitOnce = set;
    }
}
