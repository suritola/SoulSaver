using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// 무한 모드 최종 보스 「개발자」 (1.1.1): 다섯 캐릭터의 최종 진화 필살기를 차례로 돌려 씀
// 거너 총알 폭풍 · 검사 회전 베기 · 도적 출혈 돌진 · 궁수 화살비 · 연금술사 대폭발 — 거울의 군주 흉내(UltSpin …)를 더 세게 + 최종 진화 덧붙임
// 모든 판정은 그대로 경고가 먼저 보이고, 한 번 쓸 때 최대 DevMaxHits 번 · 한 대는 최대 체력의 MaxHitShare 까지 (즉사 없음)
public partial class BossSkills
{
    const int DevMaxHits = 4;
    static readonly Color Dev = new Color(1f, 0.85f, 0.35f, 0.95f);
    static readonly string[] DevHeroes = { "거너", "검사", "도적", "궁수", "연금술사", "지도 제작자", "시계공", "우체부", "유령" };     // CharacterId 순서
    int devTurn;

    void DeveloperTurn(PlayerController p)
    {
        if (Time.time < next) return;
        next = Time.time + (boss.Enraged ? 1.4f : 2.2f);
        int who = devTurn++ % DevHeroes.Length;
        if (StageManager.Instance != null)
            StageManager.Instance.ShowBanner(Loc.T("개발자가 「{0}」의 최종 진화 필살기를 쓴다!").Replace("{0}", Loc.T(DevHeroes[who])), 1.8f);
        StartCoroutine(Run(DeveloperUlt((CharacterId)who, boss.Enraged)));
    }

    IEnumerator DeveloperUlt(CharacterId who, bool strong)
    {
        ultHits = 0;
        Fx.Spawn("fx_soulburst", transform.position, 11f, Dev, 16f);
        Hostile.Play("shimmer", 0.7f, 0.55f);
        yield return Windup(Dev, 0.6f);
        if (!Alive) yield break;
        switch (who)
        {
            case CharacterId.Gunner:
                // 총알 폭풍: 도는 탄막과 함께 조준 사격
                StartCoroutine(DevBulletStorm(strong ? 5 : 4));
                yield return UltLockOn(strong ? 8 : 6);
                break;
            case CharacterId.Swordsman:
                // 회전 베기: 벨 때마다 사방으로 검기
                for (int i = 0; i < (strong ? 4 : 3) && Alive; i++)
                {
                    yield return UltSpin(1);
                    DevRing(12, 8f, i * 15f, 12f);
                }
                break;
            case CharacterId.Rogue:
                // 출혈 돌진: 여러 번 꿰뚫고, 멈춘 자리에서 표창 부채
                for (int i = 0; i < (strong ? 5 : 4) && Alive; i++)
                {
                    yield return UltDash(1);
                    DevFan(7, 50f, 10f);
                }
                break;
            case CharacterId.Archer:
                // 화살비: 더 많은 파도 + 끝에 하늘에서 쏟아지는 고리
                yield return UltRain(strong ? 5 : 4);
                if (Alive) DevRing(20, 6.5f, 0f, 12f);
                break;
            case CharacterId.Ghost:
                // 빙의: 플레이어 곁으로 스며들었다가 안에서 터지듯 + 냉기 탄
                StartCoroutine(DevBulletStorm(strong ? 3 : 2));
                yield return UltPossess(strong ? 4 : 3);
                break;
            case CharacterId.Postman:
                // 속달: 플레이어 둘레 곳곳에 표식이 붙고 한꺼번에 터짐 + 편지 탄
                StartCoroutine(DevBulletStorm(strong ? 3 : 2));
                yield return UltExpress(strong ? 14 : 10);
                break;
            case CharacterId.Clockmaker:
                // 되감기: 지난 자리마다 시계가 터짐 + 톱니 탄막
                StartCoroutine(DevBulletStorm(strong ? 3 : 2));
                yield return UltClock(strong ? 7 : 5);
                break;
            case CharacterId.Cartographer:
                // 지도 접기: 접히는 띠 사이로 잉크 탄
                StartCoroutine(DevBulletStorm(strong ? 3 : 2));
                yield return UltFold(strong ? 4 : 3);
                break;
            default:
                // 대폭발: 큰 폭발 사이사이 불꽃놀이
                StartCoroutine(DevFireworks(strong ? 8 : 6));
                yield return UltBlast(strong ? 3 : 2);
                break;
        }
    }

    // 유령 · 빙의: 보스가 흐려지며 플레이어 곁으로 스며든 뒤, 그 자리에서 영혼 폭발 (경고 원 밖으로 피함)
    IEnumerator UltPossess(int times)
    {
        Color col = boss != null && boss.developer ? Dev : Mirror;
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 at = Hostile.ClampArena(pl.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.5f));
            if (Hostile.IsWall(at)) at = transform.position;
            Fx.Spawn("fx_stealth", transform.position, 4f, col, 16f);
            transform.position = at;
            Hostile.Circle(at, 3.4f, 1f, col);
            yield return Windup(col, 1f);
            if (!Alive) yield break;
            Fx.Spawn("fx_soulburst", at, 8f, col, 16f);
            UltHit(UltInCircle(at, 3.4f));
            Hostile.Play("boom", 0.6f, 0.8f);
            Hostile.Shake(0.15f);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 우체부 · 속달: 플레이어 둘레 곳곳에 편지 표식, 1.3초 뒤 한꺼번에 터짐 (빈자리로 피함)
    IEnumerator UltExpress(int marks)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        Color col = boss != null && boss.developer ? Dev : Mirror;
        Vector3 c = pl.transform.position;
        List<Vector3> spots = new List<Vector3> { c };
        for (int i = 1; i < marks; i++) spots.Add(Hostile.ClampArena(c + (Vector3)(Random.insideUnitCircle * 6f)));
        foreach (Vector3 p in spots) Hostile.Circle(p, 1.7f, 1.3f, col);
        Hostile.Play("pop", 0.5f, 1.6f);
        yield return Windup(col, 1.3f);
        if (!Alive) yield break;
        bool inside = false;
        foreach (Vector3 p in spots)
        {
            Fx.Spawn("fx_explosion", p, 3.4f, col, 20f);
            inside |= UltInCircle(p, 1.7f);
        }
        UltHit(inside);
        Hostile.Play("boom", 0.6f, 1.2f);
        yield return new WaitForSeconds(0.3f);
    }

    // 시계공 · 되감기: 플레이어가 지나간 자리마다 시계 표식, 1.2초 뒤 「과거」가 그 자리에서 터짐 (계속 움직이면 피함)
    IEnumerator UltClock(int marks)
    {
        Color c = boss != null && boss.developer ? Dev : Mirror;
        for (int i = 0; i < marks && Alive; i++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector3 at = pl.transform.position;
            Hostile.Circle(at, 2.2f, 1.2f, c);
            Fx.Spawn("fx_target_rune", at, 4.4f, c, 12f);
            Hostile.Play("clank", 0.3f, 2.2f);
            StartCoroutine(ClockBlast(at, c));
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(1.2f);
    }

    IEnumerator ClockBlast(Vector3 at, Color c)
    {
        yield return new WaitForSeconds(1.2f);
        if (!Alive) yield break;
        Fx.Spawn("fx_explosion", at, 4.6f, c, 18f);
        UltHit(UltInCircle(at, 2.2f));
        Hostile.Play("chime", 0.4f, 0.6f);
    }

    // 지도 제작자 · 지도 접기: 플레이어를 가로지르는 넓은 띠를 경고한 뒤 접듯이 짓이김 (띠 밖으로 나가면 피함)
    IEnumerator UltFold(int folds)
    {
        for (int n = 0; n < folds && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 c = pl.transform.position;
            Vector2 d = DirTo(pl);
            Vector2 crease = new Vector2(-d.y, d.x);
            Vector3 a = c - (Vector3)(crease * 9f), b = c + (Vector3)(crease * 9f);
            Hostile.Line(a, b, 5f, 1f, boss != null && boss.developer ? Dev : Mirror);
            yield return Windup(Mirror, 1f);
            if (!Alive) yield break;
            pl = Hostile.Player;
            Fx.Beam(a, b, 1.6f, new Color(0.75f, 0.85f, 1f, 0.95f), 0.35f, 18);
            UltHit(pl != null && Hostile.DistanceToSegment(pl.transform.position, a, b) < 2.5f);
            Hostile.Play("crack", 0.7f, 0.8f);
            Hostile.Shake(0.15f);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // 사방으로 한 바퀴
    void DevRing(int n, float speed, float offsetDeg, float damage)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (offsetDeg + i * 360f / n) * Mathf.Deg2Rad;
            Hostile.Shoot(transform.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed, Capped(damage * Power), 0.5f, Dev, 1.5f, 4f);
        }
        Hostile.Play("shimmer", 0.4f, 1.3f);
    }

    // 플레이어 쪽 부채꼴
    void DevFan(int n, float spreadDeg, float damage)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) return;
        Vector2 d = DirTo(pl);
        for (int i = 0; i < n; i++)
        {
            float off = n == 1 ? 0f : -spreadDeg * 0.5f + spreadDeg * i / (n - 1);
            Hostile.Shoot(transform.position, Rotate(d, off), 9f, Capped(damage * Power), 0.45f, Dev, 1.3f, 3f);
        }
        Hostile.Play("whoosh", 0.5f, 1.4f);
    }

    // 나선으로 도는 탄막
    IEnumerator DevBulletStorm(int waves)
    {
        for (int w = 0; w < waves && Alive; w++)
        {
            DevRing(10, 7f, w * 13f, 10f);
            yield return new WaitForSeconds(0.45f);
        }
    }

    // 플레이어 둘레에 작은 폭발이 차례로 (경고 0.9초)
    IEnumerator DevFireworks(int count)
    {
        for (int i = 0; i < count && Alive; i++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 at = Hostile.ClampArena(pl.transform.position + (Vector3)(Random.insideUnitCircle * 4f));
            StartCoroutine(DevPop(at, 1.6f));
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator DevPop(Vector3 at, float r)
    {
        Hostile.Circle(at, r, 0.9f, Dev);
        yield return new WaitForSeconds(0.9f);
        if (!Alive) yield break;
        Fx.Spawn("fx_explosion", at, r * 2.2f, Dev, 20f);
        UltHit(UltInCircle(at, r));
        Hostile.Play("boom", 0.4f, 1.3f);
    }
}
