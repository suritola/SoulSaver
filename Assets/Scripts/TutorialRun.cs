using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이 튜토리얼 (2.1.9~): 글로 읽는 대신 실제 게임 화면에서 기본 조작을 하나씩 직접 해 보며 익힘
//   이동 → 조준 · 공격 → 재장전 → 필살기 → 레벨업 카드 → 영혼 트리 → 무기 진화 → 진화한 무기로 마무리
// 조작을 익히는 게 목적이라 진짜 판처럼 싸우지 않음: 적은 따로 나오지 않고 단계마다 약한 적만 몇 마리, 플레이어는 죽지 않음
// 단계에 필요한 레벨업 포인트 · 영혼 조각 · 필살기 게이지 · 코인은 그때그때 줌 (튜토리얼 안에서만, 포인트 · 업적 · 저장은 없음)
// 언제든 오른쪽 위 [튜토리얼 그만두기]나 ESC → 메인 메뉴로 그만둘 수 있고, 다 끝나면 자동으로 메인 메뉴로 돌아감
// 거너로 진행 (재장전까지 모든 기본 조작이 있는 캐릭터)
public class TutorialRun : MonoBehaviour
{
    public static bool Active { get; private set; }
    const string SeenKey = "tutorial.play.seen";        // 한 번이라도 해 봤는지 (메인 메뉴 버튼 반짝임)
    const string DoneKey = "tutorial.play.done";
    public static bool Seen => Prefs.GetInt(SeenKey, 0) == 1;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Done = new Color(0.55f, 1f, 0.55f);

    // 단계: 제목 · 설명 (번역 키, {RELOAD} 같은 키 표시는 Loc.T 가 지금 키로 바꿈)
    static readonly (string title, string body)[] Steps =
    {
        ("이동", "{MOVE}로 이리저리 움직여 보세요."),
        ("조준 · 공격", "마우스로 조준하고 좌클릭으로 공격합니다. 누르고 있으면 계속 쏩니다. 다가오는 적을 모두 쓰러뜨리세요."),
        ("재장전", "총알은 탄창만큼만 쏠 수 있습니다. [{RELOAD}]를 눌러 재장전하세요. (다 쏘면 저절로 재장전)"),
        ("필살기", "필살기 게이지가 가득 찼습니다! 우클릭을 누르고 있으면 시간이 느려지며 적을 조준하고, 떼면 조준한 적 모두에게 쏩니다."),
        ("레벨업", "경험치가 차면 레벨업 포인트가 쌓입니다. [{INTERACT}]를 눌러 창을 열고 카드를 고르세요. (클릭 → [{INTERACT}], 더블클릭, 같은 숫자 두 번)"),     // 1.0.4~ 숫자 키 방식 통일
        ("영혼 트리", "적이 떨어뜨린 영혼 조각으로 영혼 트리의 칸을 배웁니다. [{UPGRADE}]로 트리를 열고 빛나는 칸을 하나 배운 뒤 닫으세요."),
        ("무기 진화", "보스를 쓰러뜨리면 무기가 진화합니다. 세 갈래 중 하나를 고르세요. (판마다 두 번, 되돌릴 수 없음)"),
        ("마무리", "진화한 무기로 남은 적을 모두 쓰러뜨리세요!"),
    };

    StageManager sm;
    PlayerController player;
    LevelShop shop;
    EnemySpawner spawner;
    SkillGauge gauge;
    readonly List<EnermyController> foes = new List<EnermyController>();

    GameObject ui;
    TMP_Text counter, title, body, check;
    Image bar;
    int step;
    bool finished;

    // 메인 메뉴 「튜토리얼」
    public static void Begin()
    {
        Active = true;
        Prefs.SetInt(SeenKey, 1);
        Prefs.Save();
        CharacterData.Override = CharacterId.Gunner;
        RunSave.Disabled = true;
        SceneManager.LoadScene("GameScene");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // 게임 씬을 떠나면 (그만두기 · 완료 · ESC → 메인 메뉴) 튜토리얼 설정을 되돌림
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "GameScene" || !Active) return;
            Active = false;
            CharacterData.Override = null;
            RunSave.Disabled = false;
            Time.timeScale = 1f;
        };
    }

    // StageManager.Opening 이 부름 (시작 이야기 대신)
    public static IEnumerator Run(StageManager stage)
    {
        TutorialRun t = new GameObject("TutorialRun").AddComponent<TutorialRun>();
        yield return t.Play(stage);
    }

    IEnumerator Play(StageManager stage)
    {
        sm = stage;
        player = Hostile.Player;
        shop = FindFirstObjectByType<LevelShop>();
        spawner = FindFirstObjectByType<EnemySpawner>();
        gauge = FindFirstObjectByType<SkillGauge>();
        if (shop != null) shop.CancelOpening();            // 판을 시작할 때 뜨는 첫 카드는 5단계에서 직접
        if (spawner != null) spawner.spawningEnabled = false;
        BuildUi();

        for (step = 0; step < Steps.Length; step++)
        {
            ShowStep();
            yield return StepRoutine(step);
            yield return Cleared();
        }
        finished = true;
        Prefs.SetInt(DoneKey, 1);
        Prefs.Save();
        title.text = Loc.T("튜토리얼 완료!");
        body.text = Loc.T("기본 조작을 모두 익혔습니다. 곧 메인 메뉴로 돌아갑니다.");
        counter.text = "";
        bar.fillAmount = 1f;
        Hostile.Play("chime", 0.8f, 1.2f);
        Hostile.Play("levelup", 0.6f, 1f);
        yield return new WaitForSecondsRealtime(3.5f);
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator StepRoutine(int i)
    {
        switch (i)
        {
            case 0:     // 이동: 조금 돌아다니면 끝
                {
                    Vector3 last = player.transform.position;
                    float moved = 0f;
                    while (moved < 12f)
                    {
                        moved += Vector2.Distance(player.transform.position, last);
                        last = player.transform.position;
                        SetProgress(moved / 12f);
                        yield return null;
                    }
                    break;
                }
            case 1:     // 공격: 약한 적 셋
                yield return Fight(3);
                break;
            case 2:     // 재장전: 탄창을 조금 비워 두고 직접 장전
                {
                    player.NowBullet = Mathf.Min(player.NowBullet, Mathf.Max(1, player.MaxBullet / 2));
                    while (!player.IsReloading) { SetProgress(0f); yield return null; }
                    while (player.IsReloading) yield return null;
                    break;
                }
            case 3:     // 필살기: 게이지를 채워 주고 적 넷 (조준할 적이 있어야 함)
                {
                    SpawnFoes(4);
                    if (gauge != null) gauge.AddSkillPoint(gauge.MaxSkillPoint);
                    while (gauge != null && gauge.IsFull()) { SetProgress(0f); RefillGaugeIfIdle(); yield return null; }
                    yield return new WaitForSeconds(1.2f);
                    ClearFoes();
                    break;
                }
            case 4:     // 레벨업: 포인트 하나를 주고 카드를 고를 때까지
                {
                    int before = player.level;
                    if (shop != null) shop.GrantLevels(player, 1);
                    while (player.level <= before) { SetProgress(0f); yield return null; }
                    break;
                }
            case 5:     // 영혼 트리: 조각을 넉넉히 주고, 한 칸 배우고 트리를 닫을 때까지
                {
                    int spent = SoulShards.Spent;
                    SoulShards.Add(400, player.transform.position, false);
                    while (SoulShards.Spent <= spent) { SetProgress(0f); yield return null; }
                    SetProgress(0.6f);
                    while (Time.timeScale == 0f) yield return null;
                    break;
                }
            case 6:     // 무기 진화: 보스를 쓰러뜨렸을 때와 같은 진화 창
                {
                    SpecialAbilities sp = SpecialAbilities.SharedInstance;
                    int tier = sp != null ? sp.EvolutionTier : 0;
                    yield return new WaitForSecondsRealtime(1.2f);      // 설명을 읽을 틈
                    yield return sm.Evolution(false);
                    while (sp != null && sp.EvolutionTier <= tier) yield return null;
                    while (Time.timeScale == 0f) yield return null;
                    break;
                }
            case 7:     // 마무리: 진화한 무기로 다섯
                yield return Fight(5);
                break;
        }
    }

    // 단계를 마침: 초록 체크 · 소리 · 잠깐 쉼
    IEnumerator Cleared()
    {
        SetProgress(1f);
        check.text = "✔ " + Loc.T("완료!");
        check.color = Done;
        Hostile.Play("chime", 0.5f, 1.4f);
        yield return new WaitForSecondsRealtime(1.1f);
        check.text = "";
    }

    IEnumerator Fight(int count)
    {
        SpawnFoes(count);
        while (true)
        {
            foes.RemoveAll(f => f == null || f.IsDead);
            SetProgress(1f - foes.Count / (float)count);
            if (foes.Count == 0) break;
            yield return null;
        }
    }

    // 약한 적: 그 장의 첫 번째 적을 체력 낮게 · 닿아도 아프지 않게 · 기술 없이 (경험치 · 코인도 없음)
    void SpawnFoes(int count)
    {
        if (spawner == null || spawner.Stage.enemies == null || spawner.Stage.enemies.Length == 0) return;
        GameObject prefab = spawner.Stage.enemies[0];
        Vector3 me = player.transform.position;
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector3 at = Hostile.ClampArena(me + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 7f);
            GameObject go = Instantiate(prefab, at, Quaternion.identity);
            foreach (EnemySkill s in go.GetComponents<EnemySkill>()) Destroy(s);
            EnermyController e = go.GetComponent<EnermyController>();
            if (e == null) continue;
            e.setEnemyHP = 2;
            e.contactDamage = 0f;
            e.expReward = 0;
            e.coinDrop = 0;
            e.speed *= 0.6f;
            foes.Add(e);
        }
    }

    void ClearFoes()
    {
        foreach (EnermyController f in foes) if (f != null) Destroy(f.gameObject);
        foes.Clear();
    }

    // 필살기를 쓰기 전에 적을 평타로 다 잡아 버리면 조준할 적이 없으므로 다시 불러 줌
    void RefillGaugeIfIdle()
    {
        foes.RemoveAll(f => f == null || f.IsDead);
        if (foes.Count == 0) SpawnFoes(4);
    }

    void Update()
    {
        if (finished || player == null) return;
        // 죽지 않음 (조작을 익히는 게 목적), 적은 따로 나오지 않음
        if (player.PlayerHealth < player.PlayerMaxHealth) player.PlayerHealth = player.PlayerMaxHealth;
        if (spawner != null && spawner.spawningEnabled) spawner.spawningEnabled = false;
        if (ui != null && bar != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            title.color = Color.Lerp(Gold, Color.white, pulse * 0.35f);
        }
    }

    // ================================================================= 화면 (위 가운데 안내 띠 · 오른쪽 위 그만두기)
    void BuildUi()
    {
        UIKit.EnsureStyle();
        Canvas canvas = UIKit.HudCanvas();
        RectTransform panel = UIKit.Rect("TutorialPanel", canvas.transform, Vector2.zero, new Vector2(1000f, 150f));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -110f);
        ui = panel.gameObject;
        Image bg = ui.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.08f, 0.06f, 0.12f, 0.88f);
        bg.raycastTarget = false;

        counter = UIKit.Text(panel, "", 22f, new Color(0.75f, 0.7f, 0.85f), new Vector2(-400f, 48f), new Vector2(180f, 34f), TextAlignmentOptions.Left);
        title = UIKit.Text(panel, "", 34f, Gold, new Vector2(0f, 44f), new Vector2(600f, 46f));
        check = UIKit.Text(panel, "", 28f, Done, new Vector2(390f, 46f), new Vector2(220f, 40f), TextAlignmentOptions.Right);
        body = UIKit.Text(panel, "", 24f, Parch, new Vector2(0f, -10f), new Vector2(940f, 64f));
        body.enableWordWrapping = true;
        body.enableAutoSizing = true;
        body.fontSizeMin = 16f;
        body.fontSizeMax = 24f;
        foreach (TMP_Text t in new[] { counter, title, check, body }) UIKit.Forget(t);

        RectTransform track = UIKit.Rect("Track", panel, new Vector2(0f, -58f), new Vector2(900f, 10f));
        Image tb = track.gameObject.AddComponent<Image>();
        tb.color = new Color(1f, 1f, 1f, 0.12f);
        tb.raycastTarget = false;
        RectTransform fill = UIKit.Rect("Fill", track, Vector2.zero, new Vector2(900f, 10f));
        bar = fill.gameObject.AddComponent<Image>();
        bar.sprite = UIKit.ButtonSprite;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.color = Gold;
        bar.raycastTarget = false;

        Button stop = UIKit.MakeButton(canvas.transform, "튜토리얼 그만두기", Vector2.zero, new Vector2(300f, 60f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }, 24f);
        RectTransform sr = (RectTransform)stop.transform;
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(1f, 1f);
        sr.anchoredPosition = new Vector2(-30f, -110f);
    }

    void ShowStep()
    {
        counter.text = Loc.T("튜토리얼") + "  " + (step + 1) + " / " + Steps.Length;
        title.text = Loc.T(Steps[step].title);
        body.text = Loc.T(Steps[step].body);
        check.text = "";
        SetProgress(0f);
    }

    void SetProgress(float t)
    {
        if (bar != null) bar.fillAmount = Mathf.Clamp01(t);
    }
}
