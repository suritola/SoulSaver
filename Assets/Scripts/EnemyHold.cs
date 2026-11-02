using System.Collections.Generic;
using UnityEngine;

// 2.0.0~ 적 붙잡기 · 옮기기 공통 장치 (docs/plans/2026-11-02-big-update/heroes-common.md 2절)
//   첫 사용자는 씨름꾼 (들어 올림 · 던짐). 인형사 · 공사 반장 · 장의사도 같은 장치를 씀
//   EnermyController.holder 가 null 이 아니면 그 적은 스스로 걷지 · 치지 · 스킬을 쓰지 않음 (접촉 피해 · 밀어내기도 건너뜀)
//   옮기는 동안 위치는 owner 가 직접 transform.position 으로 (지도 접기 MapLoopPull 과 같은 방식) — 경기장 밖으로 내보내지 않게 Hostile.ClampArena 로 자름
//   보스(bosss) 는 EnermyController 가 아니라 잡히지 않음 · 중간 보스는 allowMid 일 때만 · 죽거나 풀로 돌아가면 holder 는 저절로 풀림 (OnGone)
public static class EnemyHold
{
    // 잡힌 적 (owner 별 정리 · 판 끝 정리용)
    static readonly List<EnermyController> held = new List<EnermyController>(32);

    // 잡혔을 때 되돌릴 값 (정렬 순서 · 그림자)
    sealed class Saved { public int layerId, order; public bool shadow; public SpriteRenderer sr; }
    static readonly Dictionary<EnermyController, Saved> saved = new Dictionary<EnermyController, Saved>();

    public static int HeldCount => held.Count;

    public static bool IsHeld(EnermyController e) => e != null && e.holder != null;

    // 잡을 수 있는 적인지 (거절 사유: 죽음 · 이미 잡힘 · 부스러기 · 시전 중 · 콜라이더 꺼짐 · 중간 보스)
    public static bool CanTake(EnermyController e, bool allowMid = false)
    {
        if (e == null || e.IsDead || e.holder != null || e.swarm != null || e.casting || !e.isActiveAndEnabled) return false;
        if (e.IsMidBoss && !allowMid) return false;
        Collider2D col = e.GetComponent<Collider2D>();
        return col == null || col.enabled;      // 땅속 · 사망 연출 중인 적은 콜라이더가 꺼져 있음
    }

    public static bool TryTake(EnermyController e, object owner, bool allowMid = false)
    {
        if (owner == null || !CanTake(e, allowMid)) return false;
        e.holder = owner;
        held.Add(e);
        return true;
    }

    // 머리 위에서 보이게: 정렬 순서를 기준 몸 위로 덮어쓰고(Release 에서 되돌림), 발밑 그림자는 숨김
    public static void Raise(EnermyController e, SpriteRenderer above, int plus = 2)
    {
        if (e == null || above == null) return;
        SpriteRenderer sr = e.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        if (!saved.ContainsKey(e))
        {
            Transform sh = e.transform.Find("FootShadow");
            saved[e] = new Saved { layerId = sr.sortingLayerID, order = sr.sortingOrder, shadow = sh != null && sh.gameObject.activeSelf, sr = sr };
            if (sh != null) sh.gameObject.SetActive(false);
        }
        sr.sortingLayerID = above.sortingLayerID;
        sr.sortingOrder = above.sortingOrder + plus;
    }

    public static void Release(EnermyController e, object owner)
    {
        if (e == null || e.holder == null || (owner != null && e.holder != owner)) return;
        e.holder = null;
        held.Remove(e);
        Restore(e);
    }

    // owner 가 잡은 모든 적 놓기 (판 끝 · 영웅 정리 · 씬 전환 · OnDisable)
    public static void ReleaseAll(object owner)
    {
        for (int i = held.Count - 1; i >= 0; i--)
        {
            EnermyController e = held[i];
            if (e == null) { held.RemoveAt(i); continue; }
            if (owner == null || e.holder == owner) { e.holder = null; held.RemoveAt(i); Restore(e); }
        }
    }

    // 적이 죽었거나 풀로 돌아감 (EnermyController 가 부름) — 빈손이 되도록 holder 만 풀고 목록에서 뺌
    public static void OnGone(EnermyController e)
    {
        if (e == null || e.holder == null) return;
        e.holder = null;
        held.Remove(e);
        Restore(e);
    }

    static void Restore(EnermyController e)
    {
        if (e == null || !saved.TryGetValue(e, out Saved s)) return;
        saved.Remove(e);
        if (s.sr != null) { s.sr.sortingLayerID = s.layerId; s.sr.sortingOrder = s.order; }
        if (s.shadow) { Transform sh = e.transform.Find("FootShadow"); if (sh != null && !e.IsDead) sh.gameObject.SetActive(true); }
    }

    // 2.0.0~ 한 곳에서 옮기기 (공사 반장 더미): 위치를 정하고 남은 속도를 지움 — Rigidbody 가 남아 있어 다른 적이 더미에 막힘 (더미가 움직이는 벽이 됨)
    public static void Place(EnermyController e, Vector3 pos)
    {
        if (e == null) return;
        pos.z = 0f;
        e.transform.position = pos;
        Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.position = pos; rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
    }

    // 벽 · 구조물 밖의 가장 가까운 빈자리 (던진 적이 착지할 때)
    public static Vector3 FreeSpot(Vector3 p)
    {
        p = Hostile.ClampArena(p);
        if (!Hostile.IsWall(p)) return p;
        for (float r = 0.5f; r <= 4f; r += 0.5f)
            for (int k = 0; k < 12; k++)
            {
                float a = k * 30f * Mathf.Deg2Rad;
                Vector3 q = Hostile.ClampArena(p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r);
                if (!Hostile.IsWall(q)) return q;
            }
        return p;
    }
}
