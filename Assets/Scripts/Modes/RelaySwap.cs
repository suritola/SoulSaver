using UnityEngine;

// 2.0.0~ 영혼 릴레이: 판 중간에 영웅 바꾸기 (modes.md 1-8 「RelaySwap.Run」 순서) — 가장 어려운 부분
//   논리 교대 전부가 한 프레임 안에서 끝남 (게임이 멈춰도 상태가 반쯤 바뀐 채 남지 않게) · 연출은 RelayRun.HandoffFx 가 따로
//   replaying: 이어하기 기록 재생에서 hz<번호> 한 줄마다 같은 교대를 되풀이 (체력 · 게이지 · 환급 · 연출 · 저장은 건너뜀 — 재생이 끝난 뒤 저장값으로)
public static class RelaySwap
{
    public static void Run(PlayerController p, CharacterId next, bool replaying)
    {
        if (p == null) return;
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>();
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        CharacterId oldId = CharacterData.Selected;
        CharacterDef old = CharacterData.Current;
        CharacterKit oldKit = p.GetComponent<CharacterKit>();
        bool oldIsKit = oldId != CharacterId.Gunner;
        int dropped = 0, owed = 0, refundPoints = 0;
        RelayRun.Swapping = true;
        try
        {
            // 1. 멈출 것 멈추기: 같은 프레임의 다음 피해가 새 영웅을 다시 죽이지 않게 무적부터
            if (!replaying) p.GrantInvincibility(3f);
            Safe(() => p.CancelSkill());
            Safe(() => { if (oldKit != null) oldKit.CancelUltForPause(); });
            Safe(() => GameInput.ResetAttackLock());
            if (oldKit != null) Safe(() => EnemyHold.ReleaseAll(oldKit));
            Safe(() => Paths.End());            // 갈래 효과가 바꿔 둔 값(도적 광란 탄창 등)은 아래 배율 되돌림 전에 거둠

            // 2. 앞 영웅 배율 빼기 (거너는 체력 · 공격 속도 · 이동을 곱하지 않음 — CharacterKit.ApplyDef 만 곱함)
            p.damage /= Mathf.Max(0.01f, old.damage);
            if (oldIsKit)
            {
                p.PlayerMaxHealth /= Mathf.Max(0.01f, old.hp);
                p.ShootSpeed *= Mathf.Max(0.1f, old.attackSpeed);
                p.speed /= Mathf.Max(0.01f, old.move);
            }
            p.BaseMoveSpeed /= Mathf.Max(0.01f, old.move);
            p.MaxBullet = RelayRun.BaseMag;
            p.reloadTime = RelayRun.BaseReload;
            if (sp != null) sp.RescaleBase(1f / Mathf.Max(0.01f, old.damage));

            // 3. 카드 정리 (영웅 카드 · 고유 스킬 · 갈래 → 단계 수를 셈) · 5. 트리 영웅 칸 · 특수 능력 (진화를 되돌리기 전 — 트리 칸 비용을 지금 영웅 기준으로 읽음)
            if (shop != null) Safe(() => shop.RelayDropHero(out dropped));
            if (sp != null)
            {
                Safe(() => sp.RelayDropHeroNodes());
                Safe(() => refundPoints = sp.RelayUnequipAll());
                // 4. 진화 정리: 1 · 2차는 버리고 같은 단계 수를 다시 고르게 (각성은 이어짐)
                Safe(() => sp.RelayResetEvolution(out owed));
            }
            StageManager sm = StageManager.Instance;
            if (sm != null && refundPoints > 0) sm.specialPoints += refundPoints;       // 내려놓은 특수 능력은 포인트로 돌려줌 (새 영웅의 것을 다시 고름)

            // 6. 키트 갈아 끼우기: DestroyImmediate — Instance 가 같은 프레임에 새 키트를 가리켜야 하고 OnDestroy 정리(호문쿨루스 · 도형 · 톱니 …)가 지금 끝나야 함
            if (oldKit != null) Object.DestroyImmediate(oldKit);
            if (replaying) RelayRun.ReplayAdvance();
            else CharacterData.Override = next;
            CharacterDef nd = CharacterData.Current;
            if (next != CharacterId.Gunner)
            {
                Animator anim = p.GetComponent<Animator>();
                CharacterKit.Attach(p);
                CharacterKit kit = p.GetComponent<CharacterKit>();
                if (kit != null) kit.ApplyDef();      // Start(다음 프레임)를 기다리면 이어하기 재생이 넣은 체력을 PlayerHealth = Max 가 덮음
                else Debug.LogWarning("[RelaySwap] 키트가 붙지 않았다: " + next);
                if (anim != null) anim.enabled = false;
            }
            else
            {
                p.ApplyGunnerDef();
                Animator anim = p.GetComponent<Animator>();
                if (anim != null) anim.enabled = true;       // 키트가 껐던 거너 애니메이터 (그림은 애니메이터가 다시 고름)
            }
            p.BaseMoveSpeed *= Mathf.Max(0.01f, nd.move);
            p.NowBullet = p.MaxBullet;
            if (sp != null) sp.RescaleBase(Mathf.Max(0.01f, nd.damage));

            // 7. 다시 캐시: 손에 든 무기 · 발 그림자 · 고유 스킬 · 갈래 · 카드 닫힘 (영웅을 Start/Awake 에서 한 번만 읽는 곳)
            Safe(() => PlayerLook.Rebind());
            if (shop != null) Safe(() => shop.RelayAfterHeroChange());      // Paths.BeginRun · SignatureSkills.Rebind 포함

            // 8. 체력 · 게이지 · 유품 · 환급
            RelayHeirlooms.Add(oldId);
            if (!replaying)
            {
                p.PlayerHealth = p.PlayerMaxHealth;
                SkillGauge g = Object.FindFirstObjectByType<SkillGauge>();
                if (g != null) g.AddSkillPoint(g.MaxSkillPoint);      // 필살기 게이지 가득 — 「죽음을 쓰는」 보상
                PlayerHP hp = Object.FindFirstObjectByType<PlayerHP>();
                if (hp != null) hp.ResetFeel();
                RelayRun.FreeLeft += Mathf.Min(12, Mathf.CeilToInt(dropped * 0.5f));
                RelayRun.EvoOwed += owed;
            }
        }
        finally { RelayRun.Swapping = false; }

        // 9. 기록 · 저장: 쓰러지는 순간 끄면 앞 영웅이 살아나는 꼼수가 없게 기록 직후 바로 씀 (재생 중에는 Record · StageForSave 가 저절로 건너뜀)
        RunSave.Record("hz" + RelayRun.Index);
        RunSave.StageForSave();
        if (!replaying) Prefs.Save();
    }

    static void Safe(System.Action a)
    {
        try { a(); }
        catch (System.Exception e) { Debug.LogException(e); }      // 한 단계가 터져도 나머지 교대는 이어서 (반쯤 바뀐 채 멈추는 것보다 낫다)
    }
}
