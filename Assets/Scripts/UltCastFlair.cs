using UnityEngine;

// 필살기를 쓰는 순간 (1.0.2): 모든 캐릭터에게 캐릭터 색 충격파 · 섬광 · 빛 번짐 · 솟는 화음
// PlayerController.UltUsed 를 받아서 그림만 (판정 없음)
public static class UltCastFlair
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        PlayerController.UltUsed -= OnUlt;
        PlayerController.UltUsed += OnUlt;
    }

    static float lastAt;

    static void OnUlt()
    {
        if (Time.time - lastAt < 0.2f) return;      // 같은 순간 겹쳐 부르면 한 번만
        lastAt = Time.time;
        PlayerController p = Hostile.Player;
        if (p == null) return;
        At(p.transform.position, CharacterData.Current.color);
    }

    // 1.0.4~ 개발자(무한 모드 최종 보스)가 영웅 필살기를 흉내 낼 때도 같은 연출 (BossSkills.DeveloperFx, 그 영웅 색)
    public static void At(Vector3 at, Color c)
    {
        Color bright = Color.Lerp(c, Color.white, 0.35f);
        ShockRing.Spawn(at, 0.5f, 6f, 0.4f, new Color(bright.r, bright.g, bright.b, 0.9f), 0.3f);
        ShockRing.Spawn(at, 0.3f, 3.5f, 0.3f, new Color(1f, 1f, 1f, 0.8f), 0.18f);
        Fx.Spawn("fx_soulburst", at, 5f, new Color(bright.r, bright.g, bright.b, 0.9f), 20f);
        if (Hostile.Glow != null) FadeSprite.Spawn("UltGlow", Hostile.Glow, at, 7f / 8f, new Color(bright.r, bright.g, bright.b, 0.75f), "Effect", 30, 0.35f);
        Hostile.Play("ultcast", 0.75f, 1f);
        // 1단계-12~ 내 필살기를 쓸 때 화면은 흔들지 않음 (보는 걸 흐림) — 개발자 보스가 쓰는 건(출처 「기타」) 경고라 그대로
        bool mine = DamageSource.Current != null && DamageSource.Current != DamageSource.Other;
        if (VisualRules.Legacy || !mine) Hostile.Shake(0.12f);
    }
}
