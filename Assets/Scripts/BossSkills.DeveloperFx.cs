using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1.0.4~ 개발자(무한 모드 최종 보스)가 쓰는 영웅 필살기를 플레이어가 쓸 때와 똑같은 이펙트 · 소리 · 화면 연출로
// 플레이어 쪽 구현(PlayerController 조준 사격 · CharacterKit 회전 베기 · 출혈 돌진 · 화살비 · 대폭발 · 지도 접기 · 되감기 · 속달 · 빙의 · UltCastFlair)과
// 같은 Fx 이름 · 색 · 크기 · 소리를 쓰고, 필살기 출처(DamageSource.Ult)로 그려 크기 · 밝기 · 그리는 순서 · 빛 번짐 · 진화 단계 그림까지 같게
// 판정은 예전처럼 경고(Hostile.Circle · Line)가 먼저 보이고 UltHit 으로 횟수(DevMaxHits) · 한 대 피해(MaxHitShare) 제한 (즉사 없음)
// 플레이어에게 이롭던 효과는 뒤집음: 거너 조준 사격의 시간 느려짐 → 플레이어 · 플레이어 탄이 느려짐 (PlayerController.HostileTimeSlow, 겹쳐 쌓이지 않고 시간만 갱신)
public partial class BossSkills
{
    // CharacterKit 의 같은 이름 색과 같은 값
    static readonly Color KitSword = new Color(0.55f, 0.75f, 1f);
    static readonly Color KitDashGhost = new Color(0.6f, 0.35f, 0.9f, 0.55f);
    static readonly Color KitFlask = new Color(0.8f, 1f, 0.7f);
    static readonly Color KitInkGold = new Color(1f, 0.82f, 0.35f, 0.95f);
    static readonly Color KitTimeBlue = new Color(0.55f, 0.85f, 1f, 0.9f);
    static readonly Color KitBrass = new Color(1f, 0.78f, 0.35f);
    static readonly Color KitPostRed = new Color(0.95f, 0.35f, 0.3f);
    static readonly Color KitChill = new Color(0.6f, 0.9f, 1f, 0.9f);
    static readonly Color KitWraith = new Color(0.75f, 0.95f, 1f, 0.85f);

    // CharacterKit.DecorSize 와 같은 계산 (필살기 출처의 장식 이펙트가 화면에서 visible 크기로 보이게: 장식 1.45배 × 스킬 1.4배를 되돌림)
    static float UltDecor(float visible) => visible / (1.45f * 1.4f);

    // 필살기를 쓰는 순간: 그 영웅 색 충격파 · 섬광 · 빛 번짐 · 솟는 화음 (플레이어와 같은 UltCastFlair)
    void DevCastFlair(CharacterId who)
    {
        using (DamageSource.As(DamageSource.Ult)) UltCastFlair.At(transform.position, CharacterData.Def(who).color);
    }

    // ---------------------------------------------------------------- 거너 · 조준 사격
    // 플레이어처럼 화면이 물들고 카메라가 멀어지며 충전 소리 — 대신 느려지는 건 플레이어 · 플레이어 탄
    // 느려진 동안 플레이어 자리에 플레이어 필살기와 같은 조준 표식이 차례로 붙고(경고 원과 함께), 풀린 뒤 잠깐 있다가 표식마다 한 발씩
    const float DevAimEvery = 0.3f, DevShotGrace = 0.25f, DevShotEvery = 0.12f;

    IEnumerator DevLockOn(int marks)
    {
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        // 1.1.9~ 표식 간격 · 쉬는 틈 · 발사 간격 모두 DevTeleMul 배 (마지막 표식 경고도 DevTeleFloor 이상이 되게 쉬는 틈을 늘림)
        float k = DevTeleMul;
        float aim = DevAimEvery * k, every = DevShotEvery * k;
        float grace = Mathf.Max(DevShotGrace * k, DevTeleFloor - aim);
        pl.HostileTimeSlow(marks * aim);
        List<Vector3> spots = new List<Vector3>();
        List<GameObject> pins = new List<GameObject>();
        for (int i = 0; i < marks && Alive; i++)
        {
            pl = Hostile.Player;
            if (pl == null) break;
            Vector3 at = pl.transform.position;
            // 이 표식이 쏘일 때까지 경고 (풀린 뒤 grace 쉬고 every 마다 한 발)
            Hostile.Circle(at, 1.5f, (marks - i) * aim + grace + i * every, Dev);
            pins.Add(DevAimMark(pl, at));
            spots.Add(at);
            Hostile.Play("zap", 0.3f, 2f);
            yield return new WaitForSeconds(aim);
        }
        if (Alive) yield return new WaitForSeconds(grace);
        for (int i = 0; i < spots.Count && Alive; i++)
        {
            if (pins[i] != null) Destroy(pins[i]);
            Vector3 at = spots[i];
            pl = Hostile.Player;
            if (pl != null) pl.PlayUltSound(false);
            using (DamageSource.As(DamageSource.Ult))
            {
                Fx.Beam(transform.position, at, 0.6f, new Color(0.85f, 0.95f, 1f), 0.2f);
                Fx.Spawn("fx_spark", at, 4f, Dev, 20f);
            }
            UltHit(UltInCircle(at, 1.5f));
            yield return new WaitForSeconds(every);
        }
        foreach (GameObject g in pins) if (g != null) Destroy(g);
    }

    // 플레이어 필살기와 같은 조준 표식 (TargetMark 프리팹) — 표식은 자리에 박혀 있고, 핀을 지우면 같이 사라짐 (보스가 쓰러져도 Own 이 지움)
    GameObject DevAimMark(PlayerController pl, Vector3 at)
    {
        GameObject pin = Own(new GameObject("DevAimPin"));
        pin.transform.position = at;
        if (pl.targetMarkPrefab != null)
        {
            GameObject mark = Instantiate(pl.targetMarkPrefab, at, Quaternion.identity, pin.transform);
            if (mark.TryGetComponent(out TargetMark tm)) tm.target = pin.transform;
        }
        return pin;
    }

    // ---------------------------------------------------------------- 검사 · 회전 베기
    // 다가설 자리에 둥근 경고 + 모으는 반짝임 · 소리(플레이어 차징과 같음), 1초 뒤 회전 베기 그림 · 충격파 · 소리
    IEnumerator DevSpin()
    {
        const float r = 4f;
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        float gap = Vector2.Distance(transform.position, pl.transform.position);
        Vector3 at = Hostile.ClampArena(transform.position + (Vector3)(DirTo(pl) * Mathf.Min(3.5f, gap)));
        if (Hostile.IsWall(at)) at = transform.position;
        float warn = DevTele(1f);
        Hostile.Circle(at, r, warn, Dev);
        Hostile.Play("hum", 0.5f, 0.8f);
        for (float t = 0f; t < warn; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            if (Random.value < 0.4f)
                using (DamageSource.As(DamageSource.Ult))
                    Fx.Spawn("fx_sparkle", transform.position + (Vector3)(Random.insideUnitCircle * 1.5f), 0.6f, KitSword, 18f);
            yield return null;
        }
        transform.position = at;
        using (DamageSource.As(DamageSource.Ult))
        {
            Fx.Spawn("fx_spinslash", at, r * 2.4f, Color.white, 22f);
            Fx.Spawn("fx_shock", at, r * 2.2f, new Color(0.6f, 0.8f, 1f, 0.8f), 20f);
        }
        UltHit(UltInCircle(at, r));
        Hostile.Play("slash", 1f, 0.8f);
        Hostile.Play("spin", 0.9f, 1f);
        Hostile.Play("whoosh", 0.6f, 0.6f);
        yield return new WaitForSeconds(0.35f);
    }

    // ---------------------------------------------------------------- 도적 · 출혈 돌진
    // 돌진 길을 먼저 그은 뒤(경고), 플레이어 돌진과 같은 연막 · 보라 잔상 · 그림자 줄기 · 소리로 꿰뚫음
    IEnumerator DevDash()
    {
        const float time = 0.32f;           // CharacterKit.BleedDash 와 같은 돌진 시간
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        Vector2 d = DirTo(pl);
        Vector3 from = transform.position;
        Vector3 to = from;
        foreach (float len in new[] { 11f, 7f, 4f })
        {
            Vector3 end = Hostile.ClampArena(from + (Vector3)(d * len));
            if (!Hostile.IsWall(end)) { to = end; break; }
        }
        float warn = DevTele(0.75f);
        Hostile.Line(from, to, 2f, warn, Dev);
        yield return Windup(Dev, warn);
        if (!Alive) yield break;
        float rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        using (DamageSource.As(DamageSource.Ult)) Fx.Spawn("fx_stealth", from, 3f, Color.white, 16f);
        Hostile.Play("whoosh", 1f, 1.4f);
        Hostile.Play("slash", 0.7f, 1.5f);
        int ghosts = 0;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            if (!Alive) yield break;
            Vector3 p = Vector3.Lerp(from, to, t / time);
            transform.position = p;
            if (t >= ghosts * 0.03f)
            {
                ghosts++;
                DevDashGhost(p, d, rot);
            }
            yield return null;
        }
        if (!Alive) yield break;
        transform.position = to;
        using (DamageSource.As(DamageSource.Ult)) Fx.Spawn("fx_stealth", to, 2.4f, new Color(1f, 0.7f, 0.8f), 18f);
        pl = Hostile.Player;
        if (UltHit(pl != null && Hostile.HurtToSegment(pl, from, to) < 1.2f)) Hostile.Shake(0.1f);
        yield return new WaitForSeconds(0.3f);
    }

    // 돌진 잔상 (CharacterKit.BleedDash 와 같은 보라 잔상 · 그림자 줄기)
    void DevDashGhost(Vector3 p, Vector2 d, float rot)
    {
        if (sr != null && sr.sprite != null)
        {
            GameObject g = SpecialAbilities.MakeSprite("DashGhost", sr.sprite, p, 1f, KitDashGhost, "Character", -1);
            g.transform.localScale = transform.lossyScale;
            g.GetComponent<SpriteRenderer>().flipX = sr.flipX;
            g.AddComponent<FadeOut>().duration = 0.3f;
        }
        using (DamageSource.As(DamageSource.Ult)) Fx.Spawn("fx_shadowdash", p - (Vector3)(d * 1.2f), 1.2f, Color.white, 24f, rot, 13);
    }

    // ---------------------------------------------------------------- 궁수 · 화살비
    // 플레이어 자리와 둘레에 떨어질 자리(경고, 사이 틈으로 피함), 활 당기는 소리 → 시위 소리와 함께 플레이어 화살비와 같은 화살이 쏟아짐
    IEnumerator DevRain(int waves)
    {
        const float r = 2.2f;
        const int n = 5;
        Vector3[] spots = new Vector3[n];
        float warn = DevTele(1f);
        for (int w = 0; w < waves && Alive; w++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 c = pl.transform.position;
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < n; i++)
            {
                float a = (start + i * 360f / (n - 1)) * Mathf.Deg2Rad;
                spots[i] = i == 0 ? c : Hostile.ClampArena(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(3.5f, 5f));
                Hostile.Circle(spots[i], r, warn, Dev);
            }
            Hostile.Play("bowdraw", 0.5f, 0.8f);
            yield return new WaitForSeconds(warn);
            if (!Alive) yield break;
            bool inside = false;
            for (int i = 0; i < n; i++) inside |= UltInCircle(spots[i], r);
            UltHit(inside);
            Hostile.Play("bowtwang", 0.8f, 0.85f);
            Hostile.Play("whoosh", 0.8f, 0.9f);
            // 자리마다 화살 넷 (플레이어 화살비처럼 0.06초 간격, 한 번에 20발)
            for (int k = 0; k < 4 && Alive; k++)
            {
                using (DamageSource.As(DamageSource.Ult))
                    for (int i = 0; i < n; i++)
                        Fx.Spawn("fx_arrowrain", spots[i] + (Vector3)(Random.insideUnitCircle * r) + Vector3.up * 1f, 2f, Color.white, 18f);
                Hostile.Play(k % 2 == 0 ? "arrowfly" : "pluck", 0.35f, Random.Range(0.8f, 1.2f));
                yield return new WaitForSeconds(0.06f);
            }
            yield return new WaitForSeconds(0.11f);
        }
    }

    // ---------------------------------------------------------------- 연금술사 · 대폭발
    // 플레이어 자리에 커다란 경고 + 부글거리는 소리, 플레이어처럼 플라스크를 던져 떨어진 자리에서 같은 폭발 · 소리 (밖으로 나가면 피함)
    const float DevFlaskFlight = 0.55f;     // CharacterKit 대폭발 플라스크 비행 시간

    IEnumerator DevBlast(int blasts)
    {
        // 1.1.9~ 경고 1.3초 × DevTeleMul, 플라스크 비행도 같은 비율 (떨어지는 순간 = 판정 = 경고가 사라지는 때)
        float warn = DevTele(1.3f);
        float flight = DevFlaskFlight * warn / 1.3f;
        for (int n = 0; n < blasts && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            float r = n == 0 ? 4.5f : 3.5f;
            Vector3 at = pl.transform.position;
            Hostile.Circle(at, r, warn, Dev);
            Hostile.Play("bubble", 0.5f, 0.8f);
            yield return Windup(Dev, warn - flight);
            if (!Alive) yield break;
            float size = r * 2.4f;
            using (DamageSource.As(DamageSource.Ult))       // 날아가는 동안 · 떨어질 때도 필살기 출처 (FlaskLob 이 만들 때의 출처를 이어 감)
                FlaskLob.Throw(transform.position, at, flight, 1.6f, KitFlask, p =>
                {
                    if (!Alive) return;
                    Fx.Spawn("fx_alchemyblast", p, size, Color.white, 16f);
                    Hostile.Play("boom", 1f, 0.9f);
                    Hostile.Play("shatter", 0.8f, 0.8f);
                    Hostile.Play("fizz", 0.7f, 0.8f);
                });
            yield return Windup(Dev, flight);
            if (!Alive) yield break;
            UltHit(UltInCircle(at, r));
            Hostile.Shake(0.25f);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // ---------------------------------------------------------------- 지도 제작자 · 지도 접기
    // 플레이어를 가로지르는 넓은 띠(경고) → 플레이어 지도 접기처럼 양쪽 띠가 접히고, 접힌 선에 같은 빛줄기 · 충격파 · 베기 · 종이 소리 (띠 밖으로 나가면 피함)
    const float DevFoldClose = 0.22f;       // CharacterKit.FoldRoutine 의 접히는 시간

    IEnumerator DevFold(int folds)
    {
        const float half = 2.5f, len = 18f;
        // 1.1.9~ 경고 1초 × DevTeleMul, 접히는 시간도 같은 비율 (접힘이 끝나는 순간 = 판정 = 경고가 사라지는 때)
        float warn = DevTele(1f);
        float close = DevFoldClose * warn;
        for (int n = 0; n < folds && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 c = pl.transform.position;
            Vector2 d = DirTo(pl);
            Vector2 crease = new Vector2(-d.y, d.x);
            Vector3 a = c - (Vector3)(crease * len * 0.5f), b = c + (Vector3)(crease * len * 0.5f);
            Hostile.Line(a, b, half * 2f, warn, Dev);
            Hostile.Play("hum", 0.5f, 1.2f);
            yield return Windup(Dev, warn - close);
            if (!Alive) yield break;
            Vector3 off = (Vector3)(d * half);
            Hostile.Play("whoosh", 0.9f, 0.7f);
            using (DamageSource.As(DamageSource.Ult))
            {
                Fx.Beam(a + off, b + off, 0.8f, new Color(0.5f, 0.65f, 1f, 0.7f), close);
                Fx.Beam(a - off, b - off, 0.8f, new Color(0.5f, 0.65f, 1f, 0.7f), close);
            }
            yield return Windup(Dev, close);
            if (!Alive) yield break;
            pl = Hostile.Player;
            UltHit(pl != null && Hostile.HurtToSegment(pl, a, b) < half);
            float rot = Mathf.Atan2(crease.y, crease.x) * Mathf.Rad2Deg;
            using (DamageSource.As(DamageSource.Ult))
            {
                Fx.Beam(a, b, 1.8f, new Color(0.75f, 0.85f, 1f, 0.95f), 0.4f, 18);
                Fx.Beam(a, b, 0.7f, KitInkGold, 0.5f, 19);
                Fx.Spawn("fx_shock", c, len * 0.8f, new Color(0.45f, 0.6f, 1f, 0.85f), 18f);
                for (int i = 0; i <= 4; i++) Fx.Spawn("fx_slash", Vector3.Lerp(a, b, i / 4f), 3f, new Color(0.8f, 0.9f, 1f), 26f, rot);
            }
            Hostile.Shake(0.22f);
            Hostile.Play("boom", 0.7f, 1.2f);
            Hostile.Play("paperfold", 1f, 0.8f);
            Hostile.Play("crack", 0.6f, 0.8f);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // ---------------------------------------------------------------- 시계공 · 되감기
    // 플레이어가 지나간 자리마다 시계 표식(경고), 1.2초 뒤 「과거」가 그 자리로 되감기며 플레이어 되감기와 같은 소용돌이 · 충격파 · 놋쇠 고리 · 소리 (계속 움직이면 피함)
    const float DevClockR = 2.2f;

    IEnumerator DevClock(int marks)
    {
        Hostile.Play("clank", 0.5f, 2.2f);
        // 1.1.9~ 표식 경고 1.2초 · 표식 간격 0.35초 모두 DevTeleMul 배
        float warn = DevTele(1.2f);
        float gap = 0.35f * DevTeleMul;
        for (int i = 0; i < marks && Alive; i++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) break;
            Vector3 at = pl.transform.position;
            Hostile.Circle(at, DevClockR, warn, Dev);
            using (DamageSource.As(DamageSource.Ult)) Fx.Spawn("fx_target_rune", at, DevClockR * 2f, new Color(1f, 0.8f, 0.4f, 0.75f), 12f);
            Hostile.Play("clank", 0.3f, 2.2f);
            StartCoroutine(DevRewindBlast(at, warn));
            yield return new WaitForSeconds(gap);
        }
        yield return new WaitForSeconds(warn);
    }

    IEnumerator DevRewindBlast(Vector3 at, float warn)
    {
        yield return new WaitForSeconds(warn);
        if (!Alive) yield break;
        using (DamageSource.As(DamageSource.Ult))
        {
            // 플레이어 되감기의 도착 연출을 표식 크기에 맞춤 (반지름 5 → 2.2 비율: 소용돌이 지름 1.2R · 충격파 지름 2R)
            Fx.Spawn("fx_vortex", at, DevClockR * 1.2f, KitTimeBlue, 18f);
            Fx.Spawn("fx_shock", at, DevClockR * 2f, new Color(0.55f, 0.85f, 1f, 0.8f), 18f);
        }
        ShockRing.Spawn(at, 0.5f, DevClockR, 0.4f, KitBrass, 0.25f);
        UltHit(UltInCircle(at, DevClockR));
        Hostile.Play("rewind", 0.5f, 1f);       // 여러 개가 잇달아 터지므로 플레이어(0.9 · 0.6)보다 작게
        Hostile.Play("bell", 0.35f, 0.8f);
    }

    // ---------------------------------------------------------------- 우체부 · 속달
    // 플레이어 둘레 곳곳에 경고 + 플레이어 속달과 같은 편지 표식이 하나씩 붙고(소리가 점점 높게), 표식이 두근거리다 1.3초 뒤 한꺼번에 같은 폭발 · 소리 (빈자리로 피함)
    IEnumerator DevExpress(int marks)
    {
        const float r = 1.7f;
        // 1.1.9~ 경고 1.3초 × DevTeleMul, 편지가 붙는 시간 · 휙 소리 시점도 같은 비율
        float wait = DevTele(1.3f), scale = wait / 1.3f, attach = 0.4f * scale;
        PlayerController pl = Hostile.Player;
        if (pl == null) yield break;
        Vector3 c = pl.transform.position;
        List<Vector3> spots = new List<Vector3> { c };
        for (int i = 1; i < marks; i++) spots.Add(Hostile.ClampArena(c + (Vector3)(Random.insideUnitCircle * 6f)));
        foreach (Vector3 p in spots) Hostile.Circle(p, r, wait, Dev);
        Hostile.Play("hum", 0.5f, 1.4f);
        List<FxAnim> icons = new List<FxAnim>();
        List<float> baseScale = new List<float>();
        bool whooshed = false;
        for (float t = 0f; t < wait; t += Time.deltaTime)
        {
            if (!Alive) { DevClearIcons(icons); yield break; }
            sr.color = Color.Lerp(Color.white, Dev, Mathf.PingPong(Time.time * 6f, 1f));      // Windup 과 같은 깜빡임
            while (icons.Count < spots.Count && t >= attach * icons.Count / spots.Count)
            {
                Vector3 p = spots[icons.Count];
                FxAnim icon;
                using (DamageSource.As(DamageSource.Ult))
                {
                    icon = Fx.Play("fx_card", p, 0.9f, KitPostRed, 12f, 0f, 24, true, -1f);
                    Fx.Spawn("fx_spark", p, 0.9f, KitPostRed, 26f);
                }
                if (icon != null) Own(icon);
                icons.Add(icon);
                baseScale.Add(icon != null ? icon.transform.localScale.x : 1f);
                Hostile.Play("pop", 0.15f, 2.2f + 0.02f * icons.Count);
            }
            if (!whooshed && t >= wait - 1f * scale) { whooshed = true; Hostile.Play("whoosh", 0.8f, 1.2f); }
            // CharacterKit.PlaceExpressIcon 처럼 두근거리며 점점 커짐
            float k = t / wait;
            for (int i = 0; i < icons.Count; i++)
                if (icons[i] != null) icons[i].transform.localScale = Vector3.one * baseScale[i] * (1f + 0.45f * k + 0.12f * Mathf.Sin(Time.time * 14f));
            yield return null;
        }
        if (Alive) sr.color = Color.white;
        DevClearIcons(icons);
        if (!Alive) yield break;
        SpecialAbilities.SharedFx?.FloatText(transform.position + Vector3.up * 1.8f, Loc.T("속달") + " ×" + spots.Count, KitPostRed, 4.5f, 0f);
        bool inside = false;
        using (DamageSource.As(DamageSource.Ult))
            foreach (Vector3 p in spots)
            {
                Fx.Spawn("fx_explosion", p, r * 2f, new Color(1f, 0.55f, 0.4f), 20f);
                Fx.Spawn("fx_sparkle", p, 2.8f, new Color(1f, 0.85f, 0.6f), 22f);
                inside |= UltInCircle(p, r);
            }
        UltHit(inside);
        Hostile.Shake(Mathf.Min(0.35f, 0.1f + 0.01f * spots.Count));
        Hostile.Play("boom", 0.8f, 1.1f);
        Hostile.Play("stamp", 0.9f, 0.7f);
        Hostile.Play("crackle", 0.7f, 1.2f);
        yield return new WaitForSeconds(0.3f);
    }

    static void DevClearIcons(List<FxAnim> icons)
    {
        foreach (FxAnim i in icons) if (i != null) Destroy(i.gameObject);
        icons.Clear();
    }

    // ---------------------------------------------------------------- 유령 · 빙의
    // 플레이어 곁 자리에 경고 → 플레이어 빙의처럼 스며들어(같은 소리) 거의 투명해진 채 영혼 빛을 두르고, 1초 뒤 같은 영혼 폭발 · 냉기 충격파 · 소리 (경고 원 밖으로 피함)
    IEnumerator DevPossess(int times)
    {
        const float R = 3.4f, slide = 0.15f;      // 스며드는 시간 (CharacterKit.PossessRoutine 과 같음)
        float inside = DevTele(slide + 1f) - slide;     // 안에 머무는 시간 1초 → 1.1.9~ 경고 전체(스며듦 + 머묾)가 DevTeleMul 배
        for (int n = 0; n < times && Alive; n++)
        {
            PlayerController pl = Hostile.Player;
            if (pl == null) yield break;
            Vector3 at = Hostile.ClampArena(pl.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.5f));
            if (Hostile.IsWall(at)) at = transform.position;
            Hostile.Circle(at, R, slide + inside, Dev);
            Hostile.Play("possess", 0.9f, 1f);
            Hostile.Play("whoosh", 0.7f, 0.5f);
            Vector3 from = transform.position;
            for (float t = 0f; t < slide; t += Time.deltaTime)
            {
                if (!Alive) yield break;
                transform.position = Vector3.Lerp(from, at, t / slide);
                yield return null;
            }
            if (!Alive) yield break;
            transform.position = at;
            FxAnim glow;
            using (DamageSource.As(DamageSource.Ult)) glow = Fx.Play("fx_soulburst", at, 2.6f, KitWraith, 12f, 0f, 22, true, inside);
            if (glow != null) { glow.follow = transform; Own(glow); }
            for (float t = 0f; t < inside; t += Time.deltaTime)
            {
                if (!Alive) break;
                sr.color = new Color(1f, 1f, 1f, 0.12f);
                yield return null;
            }
            if (glow != null) Destroy(glow.gameObject);
            if (sr != null) sr.color = Color.white;
            if (!Alive) yield break;
            using (DamageSource.As(DamageSource.Ult))
            {
                Fx.Spawn("fx_soulburst", at, UltDecor(R * 2f), KitWraith, 16f);
                Fx.Spawn("fx_shock", at, R * 2f, KitChill, 18f);
            }
            ShockRing.Spawn(at, 0.5f, R, 0.4f, KitChill, 0.25f);
            UltHit(UltInCircle(at, R));
            Hostile.Shake(0.25f);
            Hostile.Play("boom", 0.8f, 0.8f);
            Hostile.Play("wail", 0.8f, 0.8f);
            Hostile.Play("freeze", 0.7f, 0.8f);
            yield return new WaitForSeconds(0.35f);
        }
    }
}
