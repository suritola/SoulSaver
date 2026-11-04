using UnityEngine;

// 무기 스킨 칸 (3-2절): 발사 그림 · 발사음 통째 교체(SkinAudio.Adjust) · 명중음(SkinFx.OnEnemyHit) · 장전 · 무기 대기 · 하이라이트 · 진화
//   투사체 · 범위 그림 형태(W2)는 Fx.SheetFor 의 이름 매핑 (호출 줄은 그대로 — 판정 · 크기 표는 원래 이름, L2)
public partial class SkinFx
{
    void WeaponFire(Vector3 origin, Vector2 dir, int kind)
    {
        if (WeaponLook == null || kind == 1 || !SlotReady("fire.fx", 0.12f)) return;
        string fx = WeaponLook.Fx("fire");
        if (fx != null) Emit(fx, WeaponLook, origin, dir, true, 60, Bud.Rep);
    }

    // 장전 (PlayerController.Reload): 무기 스킨에 장전 칸이 있으면 그 그림 · 소리로 자리 교체 → true
    public static bool OnReload()
    {
        try { return OnReloadCore(); }
        catch (System.Exception e) { GuardFail(e); return false; }
    }

    static bool OnReloadCore()
    {
        SkinFx f = Instance;
        if (f == null || Off || WeaponLook == null || !WeaponLook.Has("reload")) return false;
        return f.Slot(WeaponSkin, WeaponLook, "reload", f.transform.position, Vector2.up, true, 30, Bud.Rare, 0.5f, 0f, SkinAudio.Prio.Acc);
    }

    // Impact 연출 순간 (Impact.Run — 창이 열렸으면 오지 않음): 필살기 = 가장자리 액자(캐릭터 영웅 이상) · 평타 단계 = 무기 하이라이트
    public static void OnImpact(ImpactTier tier, Vector3 at, Vector2 dir, bool repeat)
    {
        try { OnImpactCore(tier, at, dir, repeat); }
        catch (System.Exception e) { GuardFail(e); }
    }

    static void OnImpactCore(ImpactTier tier, Vector3 at, Vector2 dir, bool repeat)
    {
        SkinFx f = Instance;
        if (f == null || Off) return;
        if (tier == ImpactTier.Ult && CharLook != null && CharLook.Fx("edge") != null && !repeat)
            SkinEdge.Show(CharLook.Fx("edge"), CharLook.main, true);
        if (tier == ImpactTier.Attack && WeaponLook != null && f.SlotReady("high", 0.5f))
            f.Slot(WeaponSkin, WeaponLook, "high", at, dir, false, 30, Bud.Rare, 0.6f, 0f, SkinAudio.Prio.Big);
    }

    // 무기 진화 순간 (SpecialAbilities.Evolved)
    void OnEvolved(int tier)
    {
        if (Off || WeaponLook == null || !SlotReady("evo", 1f)) return;
        Slot(WeaponSkin, WeaponLook, "evo", transform.position, Vector2.up, true, 31, Bud.Rare, 0.6f, 0f, SkinAudio.Prio.Big);
    }

    // W2: 무기 스킨 형태 시트 — 「<원래 이름>__<스킨>」 시트가 Resources/FX 에 있으면 그 이름 (Fx.SheetFor 가 내 것 · 평타일 때만 부름)
    public static string WeaponSheet(string name)
    {
        if (WeaponSkin == null || Off || name == null) return null;
        return WeaponSheetName(name, WeaponSkin.id);
    }

    // 투사체 보이는 그림 (CharacterKit.Projectile · FlaskLob · BulletGlow 예광탄): 스킨 시트가 있으면 그것 · 판정은 원래 이름으로 따로 잼 (알파 모양이 같은 시트 — make_skin_weapons.py 검사)
    public static string ShotSheet(string name) => WeaponSheet(name) ?? name;

    // 원래 이름 → 스킨 시트 이름(없으면 null) — 스킨이 바뀌면 비움 (Fx.Spawn 마다 불려 문자열을 만들지 않게)
    static readonly System.Collections.Generic.Dictionary<string, string> sheetName = new System.Collections.Generic.Dictionary<string, string>();
    static string sheetSkin;
    static string WeaponSheetName(string name, string skin)
    {
        if (!ReferenceEquals(sheetSkin, skin)) { sheetName.Clear(); sheetSkin = skin; }
        if (sheetName.TryGetValue(name, out string got)) return got;
        string want = name + "__" + skin;
        Sprite[] f = Fx.Frames(want);
        got = f != null && f.Length > 0 ? want : null;
        sheetName[name] = got;
        return got;
    }
}
