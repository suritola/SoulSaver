using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2.5~ 통합 결계 게이지 · 「공간 침식」(SpaceErosion)이 쓰는 BossUltimate 쪽
// 보스(참가 보스 = 살아 있는 보스, 쓰러지는 연출 중 제외 · 개발자 · 엄마 포함)가 둘 이상이면 게이지가 하나로 합쳐짐
//   어느 보스를 때려도 같은 게이지가 참 (맞은 보스 자기 최대 체력 기준 × 0.7, 셋 이상 × 0.55, 한 프레임 +0.25 까지)
//   하나만 남으면 남은 통합 값을 그 보스가 그대로 받고, 다시 둘 이상이 되면 남아 있던 보스의 개별 값에서 시작
//   가득 차면 각자의 결계 대신 거대한 결계 하나 「공간 침식」 (SpaceErosion)
public partial class BossUltimate
{
    static bool shared;
    static float sharedGauge, sharedLastHit;
    static int sharedCount, soloSlot = -1, sharedFrame = -1, gainFrame = -1;
    static float gainThisFrame, mergedNoticeAt = -99f;
    static readonly List<bosss> participants = new List<bosss>(8);
    const float SharedGainMul2 = 0.7f, SharedGainMul3 = 0.55f, SharedFrameCap = 0.25f;

    public static bool Shared => shared;
    public static float SharedGauge => sharedGauge;
    public static int SharedCount => sharedCount;
    // 공간 침식이 펼쳐진 중 · 침식 대기 중 (대기 동안 보스가 새 스킬을 시작하지 않음 · 다음 보스가 나오지 않음)
    public static bool ErosionActive { get; private set; }
    public static bool ErosionPending { get; private set; }
    // 공간 침식 중 피격 무적 +0.25초 (PlayerController.TryHit · 상태가 남지 않게 Active 를 보고 값을 돌려줌)
    public static float ErosionHurtGrace => Active && ErosionActive ? SpaceErosion.HurtGrace : 0f;

    // 결계 · 침식 남은 시간 비율 (보스바: 결계 중 1 → 0 으로 줄어듦)
    // 컷신 · 펼침 동안은 「아직 시작 전」(WindowPending) → 가득 찬 1 (지난 결계의 창을 읽어 0 이었다가 1 로 튀지 않게)
    const float WindowPending = float.MaxValue;
    static float activeFrom, activeUntil;
    public static float RemainingFrac => !Active ? 0f : activeFrom == WindowPending ? 1f : activeUntil <= activeFrom ? 0f : Mathf.Clamp01((activeUntil - Time.time) / (activeUntil - activeFrom));
    static void SetActiveWindow(float from, float until) { activeFrom = from; activeUntil = until; }
    static void SetWindowPending() => SetActiveWindow(WindowPending, WindowPending);

    // 장면 시작 (bossbar.Start): 통합 게이지 · 대기 · 쉬는 시간 · 땅 타격 대기열을 비움 (cutSeen 은 「이번 실행에서 본 적 있음」이라 그대로)
    public static void ResetStatics()
    {
        // 1.2.8~ 결계 · 침식 · 연출 · 개발자 대기도 비움 (예전엔 OnDestroy → EndNow 에서만 풀려서, 장면 정리 중 예외가 나면 다음 판 내내 보스 스킬 · 새 보스 · 15분이 막힐 수 있었음)
        Active = false;
        ErosionActive = false;
        CutscenePlaying = false;
        devHolder = null;
        shared = false;
        sharedGauge = sharedLastHit = 0f;
        sharedCount = 0;
        soloSlot = -1;
        sharedFrame = gainFrame = -1;
        gainThisFrame = 0f;
        mergedNoticeAt = -99f;
        participants.Clear();
        ErosionPending = false;
        restUntil = 0f;
        DomainStrikes.Clear();
        BossSkills.ResetOwnStatics();       // 개발자 · 엄마 고유 공격의 정적 상태 (둘 합쳐 하나 · BossSkills.OwnKit)
    }

    // 통합 모드에서 맞음 (OnBossHit 이 개별 공식으로 gain 을 구한 뒤)
    static void SharedHit(bosss b, int slot, float gain)
    {
        float g = gain * (sharedCount >= 3 ? SharedGainMul3 : SharedGainMul2);
        if (gainFrame != Time.frameCount) { gainFrame = Time.frameCount; gainThisFrame = 0f; }
        g = Mathf.Min(g, SharedFrameCap - gainThisFrame);          // 범위 공격 하나가 넷을 동시에 맞혀도 +0.25
        if (g > 0f) { gainThisFrame += g; sharedGauge = Mathf.Min(1f, sharedGauge + g); }
        sharedLastHit = Time.time;
        // 보스전마다 적어도 한 번: 아직 결계를 펼치지 않은 보스가 40% 아래로 내려가면 곧바로 가득
        if (!usedThisFight[slot] && b.setEnemyHP > 0 && b.EnemyHealth < b.setEnemyHP * 0.4f) sharedGauge = 1f;
    }

    // 한 프레임 한 번 (아무 BossUltimate.Update 에서나) — 결계 · 침식 중과 침식 대기 중에는 전환하지 않고 숫자만 셈
    static void SharedTick()
    {
        if (sharedFrame == Time.frameCount) return;
        sharedFrame = Time.frameCount;
        participants.Clear();
        foreach (bosss b in bosss.Live) if (b != null && !b.IsDead) participants.Add(b);
        int n = participants.Count;
        sharedCount = n;
        if (Active || ErosionPending) return;
        if (!shared)
        {
            if (n == 1) soloSlot = SlotOf(participants[0]);
            else if (n >= 2)
            {
                shared = true;
                // 새로 나온 보스의 Start 가 아직일 수 있어 max 로 고르지 않고, 남아 있던 보스의 값만 (둘이 한꺼번에 나오면 0 · 지난 판 값이 섞이지 않게)
                sharedGauge = soloSlot >= 0 ? gauge[soloSlot] : 0f;
                sharedLastHit = Time.time;
                NotifyMerged();
            }
            else soloSlot = -1;
            return;
        }
        if (n >= 2)
        {
            // 3초 동안 어느 보스도 맞지 않으면 아주 서서히 (초당 0.4%)
            if (Time.time - sharedLastHit > 3f && sharedGauge < 1f) sharedGauge = Mathf.Max(0f, sharedGauge - 0.004f * Time.deltaTime);
            for (int i = 0; i < n; i++)
            {
                bosss b = participants[i];
                if (!usedThisFight[SlotOf(b)] && b.setEnemyHP > 0 && b.EnemyHealth < b.setEnemyHP * 0.4f) { sharedGauge = 1f; break; }
            }
            if (sharedGauge >= 1f && Time.time >= restUntil) SpaceErosion.Begin();
            return;
        }
        // 통합 → 개별: 남은 보스 하나가 통합 값을 그대로 받음 (둘 가운데 하나가 쓰러지는 그 프레임)
        shared = false;
        if (n == 1)
        {
            int s = SlotOf(participants[0]);
            gauge[s] = sharedGauge;
            lastHit[s] = Time.time;
            soloSlot = s;
        }
        else soloSlot = -1;
        sharedGauge = 0f;
    }

    static void NotifyMerged()
    {
        if (Time.time - mergedNoticeAt >= 10f)
        {
            mergedNoticeAt = Time.time;
            Notice.Toast(NoticeIcon.Boss, Loc.T("결계 게이지가 하나로 합쳐졌다!"), 3.5f, new Color(0.54f, 0.17f, 0.89f));
        }
        Hints.Show("bossult.shared", "보스가 둘 이상이면 결계 게이지가 하나로 합쳐집니다. 어느 보스를 때려도 같은 게이지가 차고, 가득 차면 모든 보스의 결계가 하나로 무너지는 「공간 침식」이 펼쳐집니다. 보스가 하나만 남으면 남은 게이지를 그 보스가 그대로 이어받습니다.");
    }

    // ================================================================= SpaceErosion 이 부르는 것
    internal static List<bosss> Participants => participants;
    internal static float RestUntil => restUntil;
    internal static float PowerMul => Power;
    internal static string TitleOf(int slot) => Info[Mathf.Clamp(slot, 0, Info.Length - 1)].title;
    internal static Color ColorOf(int slot) => Info[Mathf.Clamp(slot, 0, Info.Length - 1)].color;
    internal static float RoarPitch(int slot) => slot == MomSlot ? 1.15f : slot == DevSlot ? 0.95f : slot == 3 ? 0.5f : slot == 2 ? 0.75f : slot == 1 ? 0.6f : 0.8f;
    internal static void SetErosionPending(bool on) => ErosionPending = on;
    internal static void SetCutscene(bool on) => CutscenePlaying = on;

    internal static void BeginErosion()
    {
        ErosionPending = false;
        Active = true;
        ErosionActive = true;
        sharedGauge = 0f;
        SetWindowPending();        // 본 진행 15초 창은 펼침이 끝난 뒤 SetErosionWindow 한 곳에서
    }

    internal static void SetErosionWindow(float from, float until) => SetActiveWindow(from, until);

    // 침식이 끝남: 쉬는 시간 20초 · 통합 게이지 0 · 참가했던 칸 모두 게이지 0 · 결계를 쓴 것으로 (거울 군주가 참가했으면 「결계 전에」 업적 조건도 꺼짐)
    internal static void EndErosion(List<bosss> members, float rest)
    {
        sharedGauge = 0f;
        sharedLastHit = Time.time;
        foreach (bosss b in members)
        {
            if (b == null) continue;
            int s = SlotOf(b);
            gauge[s] = 0f;
            usedThisFight[s] = true;
            lastHit[s] = Time.time;
        }
        restUntil = Time.time + rest;
        ErosionActive = false;
        ErosionPending = false;
        Active = false;
    }

    // 연출 도우미 (개별 결계와 같은 것)
    internal static Canvas ErosionCanvas(string name, int order) => NewCanvas(name, order);
    internal static Image ErosionImg(RectTransform parent, string name, Color c) => Img(parent, name, c);
    internal static void ErosionStretch(RectTransform r) => Stretch(r);
    internal static float ErosionCutDt(Canvas canvas) => CutDt(canvas);
    internal static bool ErosionSkip() => SkipPressed();
    internal static void ErosionSfx(string name, float vol) => PlaySfx(name, vol);
    internal static float Ease(float k) => EaseOut(k);
    internal static void ErosionWalls(bool hide) => HideWalls(hide);

    // 어둠 메시 (개별 결계와 같은 메시 · 머티리얼 하나를 같이 씀 · 크기만 localScale 로)
    internal static MeshRenderer ErosionDark(Transform parent, float radius)
    {
        GameObject go = new GameObject("DomainDark", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * (radius / Radius);
        if (darkMesh == null) { darkMesh = BuildDarkMesh(); darkMesh.hideFlags = HideFlags.DontUnloadUnusedAsset; }
        go.GetComponent<MeshFilter>().sharedMesh = darkMesh;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (darkMaterial == null) darkMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontUnloadUnusedAsset };
        mr.sharedMaterial = darkMaterial;
        mr.sortingLayerName = "Background";
        mr.sortingOrder = 40;
        return mr;
    }

    // 어둠 색 (SetDark 와 같은 계산 · 안쪽 물듦 색을 따로 받음)
    internal static void PaintDark(MeshRenderer mr, Color tint, float k)
    {
        if (mr == null) return;
        Mesh m = mr.GetComponent<MeshFilter>().sharedMesh;
        if (m == null) return;
        int seg = m.vertexCount / 4;
        Color inner = new Color(tint.r * 0.25f, tint.g * 0.15f, tint.b * 0.3f, 0.32f * k);
        Color edge = new Color(0f, 0f, 0f, 0.55f * k);
        Color outer = new Color(0.01f, 0f, 0.02f, 0.9f * k);
        if (darkColors == null || darkColors.Length != m.vertexCount) darkColors = new Color[m.vertexCount];
        Color[] c = darkColors;
        for (int i = 0; i < c.Length; i++)
        {
            int ri = i / seg;
            c[i] = ri <= 1 ? (ri == 0 ? inner : Color.Lerp(inner, edge, 0.3f)) : outer;
        }
        m.SetColors(c);
    }

    // 결계 가장자리 중 맵 안인 자리 (EdgeInArena 와 같은 계산 · 중심을 받음)
    internal static Vector3 EdgeInArenaFrom(Vector3 center, Vector3 d, float edge)
    {
        Vector3 n = d.normalized;
        for (int step = 0; step <= 12; step++)
        {
            float a = 15f * ((step + 1) / 2) * (step % 2 == 0 ? 1f : -1f);
            Vector3 to = center + Quaternion.Euler(0f, 0f, a) * n * edge;
            if (Hostile.InArena(to)) return to;
        }
        return Hostile.ClampArena(center + n * edge);
    }

    internal static TMP_Text ErosionText(RectTransform root, string ko, float size, Color color, Vector2 pos, Vector2 box) => UIKit.Text(root, ko, size, color, pos, box);
}
