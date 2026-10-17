using System.Collections;
using TMPro;
using UnityEngine;

public class Point : MonoBehaviour
{
    public int points = 0;

    public TextMeshProUGUI TextInput;

    // 1.1~ 이번 판에 받은 클리어 보너스 (장 클리어 · 최종 클리어, 결과 화면에 따로 보여 줌)
    public static int RunBonus { get; private set; }

    // 1.1~ 장 클리어마다 200 × 장 번호, 한 판을 끝까지 깨면 1,500 (배율 없이 그대로)
    public const int ChapterBonusPer = 200;
    public const int FinalBonus = 1500;
    // 1.2~ 무한 모드 클리어 보너스 1,500 → 5,000 (아래 처치 포인트 체감과 함께 「재배치」 · 이정표 EndlessMilestones 와 같은 패치)
    public const int EndlessFinalBonus = 5000;

    // 1.2~ 무한 모드(일일 도전 포함) 일반 적 처치 포인트 체감: 이번 판 처치 2,000 뒤 ×0.5 · 8,000 뒤 ×0.2
    //   10~15분 판(≤ 2,000킬)은 그대로 · 최종 국면을 넘긴 30분 클리어 판(약 22,700킬)은 약 227,000 → 약 90,000P
    //   보스 · 중간 보스 · 클리어 보너스 · 이정표는 체감 없음 (EnermyController 의 일반 적 10P 만 AddMobPoint 로)
    public const int MobDecayFrom = 2000, MobDecayDeep = 8000;
    public const float MobDecayMul = 0.5f, MobDecayDeepMul = 0.2f;
    public static float MobKillMul => !GameMode.IsEndless ? 1f : RunStats.Kills >= MobDecayDeep ? MobDecayDeepMul : RunStats.Kills >= MobDecayFrom ? MobDecayMul : 1f;

    static readonly System.Random Rng = new System.Random();
    Coroutine pulse;
    int pulseBonus;                 // 금빛으로 보여 주는 중인 보너스 (그사이 적을 잡아 글자를 다시 써도 「+보너스」 꼬리를 유지)
    Color baseColor;
    Vector3 baseScale;

    // 새 판(GameScene)마다 0으로 (CharacterData.RunPoints 와 같은 때)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") RunBonus = 0; };
    }

    void Start()
    {
        UpdatePointText();
    }

    // ================================================================= 포인트 배율 (1.2~ 여러 배율을 이 한 곳에서 곱함)
    // 지금 곱하는 것: 난이도 × 이번 장 저주 (GameMode.PointMul) × 영혼의 무게 (SoulWeight.PointMul) × 아래 RegisterMul 로 더한 것
    // 새 배율을 더하려면 (예: 영혼 각인) 판이 시작되기 전 아무 때나 한 번:
    //     Point.RegisterMul("imprint", () => Imprint.PointMul);
    //   같은 열쇠로 다시 부르면 덮어씀 · null 을 넘기면 뺌 · 걸리지 않는 판에서는 함수가 1 을 돌려주면 됨
    static readonly System.Collections.Generic.Dictionary<string, System.Func<float>> extraMuls = new System.Collections.Generic.Dictionary<string, System.Func<float>>();

    public static void RegisterMul(string key, System.Func<float> mul)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (mul == null) extraMuls.Remove(key);
        else extraMuls[key] = mul;
    }

    public static float TotalMul
    {
        get
        {
            float m = GameMode.PointMul * SoulWeight.PointMul;
            foreach (System.Func<float> f in extraMuls.Values) m *= Mathf.Max(0f, f());
            return m;
        }
    }

    // 1.2~ 일반 적 한 마리 (무한 모드 처치 수 체감 MobKillMul 을 함께 곱함)
    // 1.2.3~ mul: 적마다 따로 거는 배율 (무한 모드 개발자 · 엄마전 약한 잡몹 0.25)
    public void AddMobPoint(int point, float mul = 1f) => AddScaled(point, MobKillMul * mul);

    public void AddPoint(int point, float mul = 1f) => AddScaled(point, mul);

    void AddScaled(int point, float extra)
    {
        // 1.1~ 포인트 배율 (1.2~ TotalMul) — 포인트를 주는 곳(boss.cs · EnermyController.cs)은 이 함수 하나만 부름
        // 소수는 확률로 반올림 (11.5 → 50% 확률로 12) · 게임 난수(UnityEngine.Random, 일일 도전 시드)는 건드리지 않게 따로 뽑음
        float v = point * TotalMul * extra;
        point = Mathf.FloorToInt(v + 0.001f);
        if (v - point > 0.001f && Rng.NextDouble() < v - point) point++;
        points += point;
        // 캐릭터 구매에 쓰는 포인트로도 쌓임 (게임을 꺼도 유지)
        CharacterData.AddPoints(point);

        // 최적화: 처치 폭발 때 한 프레임에 수십 번 글자를 다시 만들던 것을 프레임 끝 한 번으로 (꺼져 있으면 바로)
        if (isActiveAndEnabled) pointsDirty = true;
        else UpdatePointText();
    }

    bool pointsDirty;
    void LateUpdate()
    {
        if (!pointsDirty) return;
        pointsDirty = false;
        UpdatePointText();
    }

    // 1.1~ 클리어 보너스: 배율 없이 그대로 · 포인트 글자가 잠깐 금빛으로 커짐 (판 안에 Point 가 없으면 보유 포인트에만)
    // 1.2~ clearBonus = false: 영혼 계약 · 일일 도전 보상처럼 클리어가 아닌 보너스 (결과 화면 「클리어 보너스」 칸에 넣지 않음)
    public static void Bonus(int n, bool clearBonus = true)
    {
        if (n <= 0) return;
        int before = CharacterData.RunPoints;
        CharacterData.AddPoints(n);
        if (CharacterData.RunPoints == before) return;          // 튜토리얼 · 자동 플레이는 주지 않음
        if (clearBonus) RunBonus += n;
        Point p = Cache<Point>.Get;
        if (p == null) return;
        p.points += n;
        p.UpdatePointText();
        if (p.TextInput != null && p.isActiveAndEnabled)
        {
            if (p.pulse != null) p.StopCoroutine(p.pulse);
            else { p.baseColor = p.TextInput.color; p.baseScale = p.TextInput.rectTransform.localScale; }
            p.pulseBonus = n;
            p.pulse = p.StartCoroutine(p.Pulse());
            p.UpdatePointText();                                // 코루틴을 단 뒤에 써야 「+보너스」 꼬리가 붙음
        }
    }

    IEnumerator Pulse()
    {
        for (float t = 0f; t < 2.5f; t += Time.unscaledDeltaTime)
        {
            float k = t < 0.2f ? t / 0.2f : 1f - Mathf.Clamp01((t - 1.8f) / 0.7f);
            TextInput.rectTransform.localScale = baseScale * (1f + 0.25f * k);
            TextInput.color = Color.Lerp(baseColor, new Color(0.96f, 0.83f, 0.47f), k);
            yield return null;
        }
        TextInput.rectTransform.localScale = baseScale;
        TextInput.color = baseColor;
        pulse = null;
        UpdatePointText();
    }

    // 금빛 효과 도중 꺼지면 코루틴이 멈추므로 크기 · 색을 여기서 되돌림
    void OnDisable()
    {
        if (pulse == null) return;
        pulse = null;
        if (TextInput != null)
        {
            TextInput.rectTransform.localScale = baseScale;
            TextInput.color = baseColor;
        }
        UpdatePointText();
    }

    void UpdatePointText()
    {
        if (TextInput == null) return;
        pointsDirty = false;
        // 최적화: 한 번에 완성해서 대입 (예전엔 꼬리가 있으면 글자를 두 번 써 TMP 가 두 번 더러워짐)
        string s = Loc.T("포인트") + " : " + points;
        if (pulse != null) s += "  <color=#F5D478>+" + pulseBonus.ToString("N0") + "</color>";
        TextInput.text = s;      // 1.0.4~ 번역 없이 「POINT : 」였음 (판 안의 코인과 다른, 영웅 · 스킨을 사는 재화)
    }
}