using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 씨름꾼 고유 스킬 (2.0.0) · 패시브(통뼈 · 모래 근성 · 장사 밥상 · 관중의 환호)
//   wr.lasso 샅바 낚기 · wr.ringslam 장외 패대기 · wr.belly 배지기 · wr.bullrush 황소 돌진 · wr.tamp 모래판 다지기
//   wr.grit 끈기 · wr.combo 기술 연결 · wr.giant 거인 메치기 · wr.snowball 눈덩이 · wr.parade 꽃가마
// 진화: wre.strike 스트라이크 장사 · wre.dohyo 천하 도효 · wre.wall 철벽 샅바 · wre.slam 천지 메치기 · wre.bull 황금 황소
//       wre.ringstrike 장외 스트라이크 · wre.giantbull 황소 장사
// 훅은 CharacterKit.Wrestler 가 부름 (기술 연결 · 눈덩이 · 샅바 낚기의 던질 때 효과는 그쪽에서 이 파일의 값을 읽음)
public partial class SignatureSkills
{
    bool IsWr => who == CharacterId.Wrestler;

    // ---------------- 값 읽기 (키보드 앞 글자 wr. · wre.)
    public static int Lv(string key) => Instance != null ? Instance.L(key) : 0;
    public static bool IsEvo(string key) => Instance != null && Instance.IsWr && Instance.E(key);
    public static float WrAtk => Instance != null ? Instance.Atk : 1f;
    public static float WrBowlPush => 2f;
    public static float WrSnowStep => Instance != null ? V(Instance.L("wr.snowball"), 0.06f, 0.03f) : 0f;
    public static float WrLassoCooldown => Instance != null ? Mathf.Max(1f, 4f - 0.5f * (Instance.L("wr.lasso") - 1)) : 4f;
    public static bool WrParade => Instance != null && Instance.IsWr && Time.time < Instance.paradeUntil;

    float paradeUntil, bellyGiantCount, rushAt, wrLastHurt, wrGritAt, wrShieldPool, wrCheerAt, wrGritCarryEnd;
    int wrShieldLayers;

    // 던질 때 피해 배율: 기술 연결(연결당) · 꽃가마(천하장사 뒤 5초)
    public static float WrThrowMul(int combo)
    {
        if (Instance == null || !Instance.IsWr) return 1f;
        float m = 1f;
        int lc = Instance.L("wr.combo");
        if (lc > 0 && combo > 0) m *= 1f + combo * V(lc, 0.15f, 0.07f) * Instance.P("wr.combo");
        int lp = Instance.L("wr.parade");
        if (lp > 0 && Time.time < Instance.paradeUntil) m *= 1f + V(lp, 0.2f, 0.1f);
        return m;
    }

    // ---------------- 훅 (CharacterKit.Wrestler 가 부름)
    public static void WrLifted(Vector3 at) { if (Instance != null && Instance.IsWr) Instance.OnWrLifted(at); }
    public static void WrBelly(Vector3 bossAt, GameObject boss) { if (Instance != null && Instance.IsWr) Instance.OnWrBelly(bossAt, boss); }
    public static void WrRingOut(Vector3 at, int tier, int count) { if (Instance != null && Instance.IsWr) Instance.OnWrRingOut(at, tier, count); }
    public static void WrUlt() { if (Instance != null && Instance.IsWr && Instance.L("wr.parade") > 0) Instance.paradeUntil = Time.time + 5f; }
    public static void WrBlocked(Vector3 at) { }          // (자리) 방패 갈래가 쓸 훅
    public static void WrBossHit(Vector3 at) { }          // (자리) 볼이 보스에 닿음
    public static void WrPin(int n) { }                   // (자리) 핀을 맞힌 수
    public static void WrLanded(Vector3 at, int pins) { }  // (자리) 착지

    // 배지기: 적을 다 들어 올리는 순간 배로 받쳐 쿵, 둘레 1.5칸 적을 밀치고 다치게 함
    void OnWrLifted(Vector3 at)
    {
        int lv = L("wr.belly");
        if (lv <= 0 || Kit == null) return;
        Vector3 p = at + Vector3.up * 0.4f;
        float r = 1.5f * Fs("wr.belly");
        Circle(p, r, Atk * V(lv, 0.5f, 0.2f) * P("wr.belly"), 2.5f);
        Fx.Spawn("fx_shock", p, r * 2f, Tint("wr.belly", new Color(0.95f, 0.8f, 0.5f, 0.7f)), 20f);
        Hostile.Play("thump", 0.4f, 0.9f);
        Flair("wr.belly", p, 2f, new Color(0.95f, 0.8f, 0.5f));
        Impact.Hit(ImpactTier.Skill, p, Vector2.up);
    }

    // 거인 메치기: 보스 · 중간 보스에게 배지기를 세 번 할 때마다 크게 뒤집어 둘레까지 충격이 퍼짐
    void OnWrBelly(Vector3 bossAt, GameObject boss)
    {
        int lv = L("wr.giant");
        if (lv <= 0) return;
        bellyGiantCount++;
        if (bellyGiantCount % 3 != 0) return;
        float d = Atk * V(lv, 3f, 1f) * P("wr.giant");
        if (boss != null) Deal(boss, d, Vector3.up, 0.5f);
        // 2.0.1~ 둘레 충격은 보스 말고 다른 적에게만 (예전엔 Circle 이 보스까지 쳐서 보스가 두 번 맞았음)
        Circle(bossAt, 4.5f * Fs("wr.giant"), 0f, 0f, c => { if (c != null && c.gameObject != boss) Deal(c.gameObject, d * 0.4f, (c.transform.position - bossAt).normalized, 3f); });
        Fx.Spawn("fx_shock", bossAt, 9f * Fs("wr.giant"), Tint("wr.giant", new Color(1f, 0.8f, 0.4f, 0.8f)), 18f);
        Flair("wr.giant", bossAt, 4f, EvoGold);
        Hostile.Shake(0.25f);
        Hostile.Play("boom", 0.7f, 0.8f);
        Impact.Hit(ImpactTier.Skill, bossAt, Vector2.up);
    }

    // 링아웃: 장외 패대기(모래가 터져 둘레 2칸) · 천하 도효(링아웃마다 모래판 +1초 · 패대기 두 배로 넓게) · 장외 스트라이크(+0.5초) · 관중의 환호(게이지)
    void OnWrRingOut(Vector3 at, int tier, int count)
    {
        bool dohyo = E("wre.dohyo");
        int lv = L("wr.ringslam");
        if (lv > 0)
        {
            float r = 2f * (dohyo ? 2f : 1f) * Fs("wr.ringslam");
            Circle(at, r, Atk * V(lv, 0.8f, 0.3f) * P("wr.ringslam"), 2f);
            Fx.Spawn("fx_explosion", at, r * 2f, Tint("wr.ringslam", new Color(0.95f, 0.8f, 0.5f)), 20f);
            Flair("wr.ringslam", at, 2.5f, new Color(0.95f, 0.8f, 0.5f));
            Impact.Hit(ImpactTier.Skill, at, Vector2.up);
        }
        if (Kit != null)
        {
            if (dohyo) Kit.WrExtendRing(1f);
            if (E("wre.ringstrike")) Kit.WrExtendRing(0.5f);
        }
        // 관중의 환호(패시브): 링아웃시킬 때마다 필살기 게이지 (0.5초에 한 번까지 — 집단 링아웃이 게이지를 끝없이 되돌리지 않게)
        if (Sp != null && Sp.Has(SpecialAbilities.KitCheer) && Time.time >= wrCheerAt && player != null && player.skillGauge != null)
        {
            wrCheerAt = Time.time + 0.5f;
            player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * (Sp.IsEvolved(SpecialAbilities.KitCheer) ? 0.025f : 0.015f));
        }
    }

    // 처치 훅 (SignatureSkills.OnKilled): 모래판 다지기 · 장사 밥상
    void WrKilled(Vector3 pos)
    {
        if (Kit != null && L("wr.tamp") > 0 && Kit.WrInRing(pos)) Kit.WrExtendRing(V(L("wr.tamp"), 0.3f, 0.15f));
        EnermyController dying = EnermyController.Dying;
        if (dying != null && Sp != null && Sp.Has(SpecialAbilities.KitFeast) && (dying.IsMidBoss || CharacterKit.WrTier(dying) >= 2))
            Heal(Sp.IsEvolved(SpecialAbilities.KitFeast) ? 6f : 3f);
    }

    // ---------------- 맞는 피해: 통뼈(적과 부딪혀 받는 피해 −10%) · 끈기 보호막
    float WrTaken(float dmg)
    {
        if (Sp != null && Sp.Has(SpecialAbilities.KitSturdyBone) && player != null)
        {
            // 2.0.1~ 적 몸통에 부딪혀 받는 피해만 (예전엔 곁에 적만 있으면 탄 · 장판까지 모든 피해를 줄였음)
            if (EnermyController.ContactHitting) dmg *= Sp.IsEvolved(SpecialAbilities.KitSturdyBone) ? 0.82f : 0.9f;
        }
        if (wrShieldPool > 0f && dmg > 0f)
        {
            float absorbed = Mathf.Min(wrShieldPool, dmg);
            wrShieldPool -= absorbed;
            dmg -= absorbed;
            wrShieldLayers = wrShieldPool <= 0.01f ? 0 : wrShieldLayers;
            if (wrShieldPool <= 0.01f) { wrShieldPool = 0f; Fx.Spawn("fx_sparkle", player.transform.position, 2f, new Color(0.7f, 0.8f, 1f), 24f); }
        }
        return dmg;
    }

    void WrHurt(float taken)
    {
        if (taken > 0f) { wrLastHurt = Time.time; wrGritAt = Time.time + 2f; }
        if (Kit != null) Kit.WrCounterHurt();       // 2.0.1~ 되치기는 실제로 맞았을 때만 (피해 배율을 읽기만 해도 발동하던 것)
    }

    float WrRate()
    {
        if (Sp == null || !Sp.Has(SpecialAbilities.KitSandGrit) || player == null) return 1f;
        float th = Sp.IsEvolved(SpecialAbilities.KitSandGrit) ? 1.4f : 1.25f;
        return player.PlayerHealth < player.PlayerMaxHealth * 0.3f ? th : 1f;
    }

    // ---------------- 매 프레임: 끈기 · 황소 돌진
    void WrTick()
    {
        CharacterKit k = Kit;
        if (k == null || player == null) return;
        bool carrying = k.WrCarrying;
        // 끈기: 적을 들고 있는 동안 맞지 않고 2초가 지날 때마다 보호막이 한 겹 (최대 2겹)
        int lg = L("wr.grit");
        // 2.0.1~ 들기 시작부터 2초를 채워야 첫 겹 (예전엔 wrGritAt 이 지난 값이라 들자마자 생겼음)
        if (!carrying) wrGritAt = Mathf.Max(wrGritAt, Time.time + 2f);
        if (lg > 0 && carrying)
        {
            if (Time.time >= wrGritAt && wrShieldLayers < 2)
            {
                wrGritAt = Time.time + 2f;
                wrShieldLayers++;
                wrShieldPool += player.PlayerMaxHealth * V(lg, 0.03f, 0.01f) * (E("wre.giantbull") ? 2f : 1f) * P("wr.grit");
                Fx.Spawn("fx_sparkle", player.transform.position, 2f, new Color(0.7f, 0.8f, 1f), 22f);
            }
        }
        // 2.0.1~ 내려놓고 6초 지나면 보호막이 사라짐 (예전 조건 lg <= 0 은 끈기가 있으면 늘 거짓이라 영영 남았음)
        else if (!carrying && wrShieldLayers > 0 && Time.time - wrLastHurt > 6f && Time.time - wrGritCarryEnd > 6f) { wrShieldLayers = 0; wrShieldPool = 0f; }
        if (carrying) wrGritCarryEnd = Time.time;
        // 황소 돌진: 적을 든 채 0.6초 넘게 한 방향으로 달리면 들린 적으로 앞을 들이받음 (0.4초마다 · 밀침)
        int lb = L("wr.bullrush");
        if (lb > 0 && carrying && k.WrMoveTime >= 0.6f && Time.time >= rushAt)
        {
            rushAt = Time.time + 0.4f;
            Vector3 front = k.WrCarriedPos + (Vector3)(k.WrMoveDir * 1.6f);
            float kn = E("wre.wall") ? 6f : 3.5f;
            float rushDmg = Atk * V(lb, 0.4f, 0.15f) * P("wr.bullrush");
            // 2.0.1~ 철벽 샅바(진화): 들이받은 적이 핀처럼 날아감 (설명만 있고 밀침만 셌던 것)
            System.Action<Collider2D> pin = E("wre.wall") ? c => { if (c != null && c.TryGetComponent(out EnermyController pe)) k.WrRushPin(pe, k.WrMoveDir, rushDmg * 0.6f); } : null;
            Circle(front, 1.7f * Fs("wr.bullrush"), rushDmg, kn, pin);
            Fx.Spawn("fx_shock", front, 3.4f, Tint("wr.bullrush", new Color(0.95f, 0.8f, 0.5f, 0.7f)), 20f);
            Hostile.Play("thump", 0.35f, 1.1f);
            Impact.Hit(ImpactTier.Skill, front, k.WrMoveDir);
        }
    }

    string WrStack(string key) => key switch
    {
        "wr.grit" => wrShieldLayers > 0 ? "x" + wrShieldLayers : null,
        "wr.parade" => Time.time < paradeUntil ? (paradeUntil - Time.time).ToString("0.0") + "s" : null,
        _ => null,
    };
}
