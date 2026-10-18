using UnityEngine;

// 검사 갈래 (2026-10-19 · docs/review/2026-10-19-빌드-시너지-기획.md 3-2): 평타(Swing)에 끼우는 자리 · 갈래 효과가 쓰는 public 래퍼
// 효과 본체는 Paths/Paths.Swordsman.cs (SwordsmanPaths). 이 파일은 CharacterKit 의 private 값(Damage · UltMul · stillTime · charge · Projectile)을 꺼내 줄 뿐
// 상태를 두지 않음 (갈래 상태는 판마다 새로 만드는 SwordsmanPaths 에) · 다른 영웅 partial 과 이름이 겹치지 않게 모두 Sword 로 시작
public partial class CharacterKit
{
    // ---------------- Swing 이 부르는 자리 (CharacterKit.cs 의 검사 평타 안 두 줄)
    // 휘두르기 직전: 사거리 · 부채꼴 반각 · 피해 배율을 갈래가 바꿈 (검기 −20% · 회오리 원형 · 피의 맹세 · 역습 자세 …)
    void SwordPathPre(Vector2 dir, ref float reach, ref float half, ref float sig)
    {
        SwordsmanPaths sp = SwordsmanPaths.Cur;
        if (sp != null && Id == CharacterId.Swordsman) sp.SwingPre(this, dir, ref reach, ref half, ref sig);
    }

    // 휘두른 뒤: 검기 방출 · 피의 파동 · 미끄러지기 · 흡혈 2배 …
    void SwordPathPost(Vector3 origin, Vector2 dir, float reach, int hits)
    {
        SwordsmanPaths sp = SwordsmanPaths.Cur;
        if (sp != null && Id == CharacterId.Swordsman) sp.SwingPost(this, origin, dir, reach, hits);
    }

    // 검기 갈래: 갈래 검기가 「날아가는 검기」 카드의 검기를 대신함 (같은 자리에 두 줄이 겹쳐 날지 않게) · 근접 특성(휘두르는 동안 받는 피해 절반)이 사라짐
    bool SwordPathWave => Id == CharacterId.Swordsman && Paths.Has(SwordsmanPaths.Wave);
    bool SwordPathNoGuard => SwordPathWave;

    // ---------------- 갈래 효과가 쓰는 래퍼
    public float SwordDamage => Damage;                         // 평타 한 번의 피해 (「베기 피해 n%」의 기준)
    public float SwordUltMul => UltMul;                         // 회전 베기 세기 · 범위 배율
    public bool SwordMoving => stillTime <= 0.1f;               // 움직이는 중 (멈춰 있던 시간으로)
    public float SwordCharge { get => charge; set => charge = Mathf.Clamp01(value); }      // 회전 베기 모은 정도 (받아치기 대가)
    public float SwordRange => def.range * reachMul;            // 기본 베기 사거리 (형태 · 연속 베기 빼고)
    public Sprite SwordBodySprite => body != null ? body.sprite : null;
    public bool SwordFlipX => body != null && body.flipX;

    // 갈래 검기 · 피의 파동 (평타 검기와 같은 그림 · 판정) — source: 판 기록 dealt_by 이름 (갈래 효과 명중을 가려내는 데도 씀)
    public Bullet SwordWave(Vector3 start, Vector2 dir, float dmg, float range, Color tint, string source)
    {
        Bullet b = Projectile(start, dir, dmg, 9999, 22f, range, "fx_swordwave", 3.4f, tint, false);
        if (b != null && !string.IsNullOrEmpty(source)) b.SetSource(source);
        return b;
    }
}
