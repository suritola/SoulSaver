using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이 튜토리얼 (2.1.9~): 글로 읽는 대신 실제 게임 화면에서 기본 조작을 하나씩 직접 해 보며 익힘
//   이동 → 조준 · 공격 → 재장전 → 필살기 → 레벨업 카드 → 영혼 트리 → 무기 진화 → 경고 피하기(1.0.4~) → 진화한 무기로 마무리
// 조작을 익히는 게 목적이라 진짜 판처럼 싸우지 않음: 적은 따로 나오지 않고 단계마다 약한 적만 몇 마리, 플레이어는 죽지 않음
// 단계에 필요한 레벨업 포인트 · 영혼 조각 · 필살기 게이지 · 코인은 그때그때 줌 (튜토리얼 안에서만, 포인트 · 업적 · 저장은 없음)
// 언제든 오른쪽 위 [튜토리얼 그만두기](확인 창)나 ESC → 메인 메뉴로 그만둘 수 있고, [이 단계 건너뛰기]로 아는 단계를 넘김
// 1.0.4~: 다 끝나면 완료 화면에서 「바로 첫 판 시작 / 메인 메뉴로」를 고름, 단계마다 해당 HUD 를 금빛 테두리로 짚어 줌
// 거너로 진행 (재장전까지 모든 기본 조작이 있는 캐릭터)
public class TutorialRun : MonoBehaviour
{
    public static bool Active { get; private set; }
    const string SeenKey = "tutorial.play.seen";        // 한 번이라도 해 봤는지
    const string DoneKey = "tutorial.play.done";
    public static bool Seen => Prefs.GetInt(SeenKey, 0) == 1;
    // 1.0.4~: 끝까지 마쳤는지 (메인 메뉴 버튼 반짝임 · 완료 체크는 이것으로, 시작만 하고 나가면 계속 반짝임)
    public static bool Done => Prefs.GetInt(DoneKey, 0) == 1;
    // 1.0.4~: 완료 화면 「바로 첫 판 시작」 → 메인 메뉴를 거쳐 게임 시작 버튼을 대신 누름 (MenuExtras.InstallMainMenu)
    static bool quickStart;
    public static bool ConsumeQuickStart() { bool q = quickStart; quickStart = false; return q; }

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color DoneColor = new Color(0.55f, 1f, 0.55f);

    // 단계: 제목 · 설명 (번역 키, {RELOAD} 같은 키 표시는 Loc.T 가 지금 키로 바꿈)
    static readonly (string title, string body)[] Steps =
    {
        ("이동", "{MOVE}로 이리저리 움직여 보세요."),
        ("조준 · 공격", "조준하고 [{ATTACK}]로 공격합니다. 누르고 있으면 계속 쏩니다. 다가오는 적을 모두 쓰러뜨리세요."),
        ("재장전", "총알은 탄창만큼만 쏠 수 있습니다. [{RELOAD}]를 눌러 재장전하세요. (다 쏘면 저절로 재장전)"),
        ("필살기", "필살기 게이지가 가득 찼습니다! [{ULT}]를 누르고 있으면 시간이 느려지며 적을 조준하고, 떼면 조준한 적 모두에게 쏩니다."),
        // 1.0.4~: 레벨업 글은 핵심 하나만 (고르는 방법은 카드 창 아래 안내), 용어는 「레벨업 카드」
        ("레벨업", "경험치가 차면 레벨업 카드를 고를 수 있습니다. [{INTERACT}]를 눌러 창을 열고 카드 한 장을 고르세요."),
        ("영혼 트리", "적이 떨어뜨린 영혼 조각으로 영혼 트리의 칸을 배웁니다. [{UPGRADE}]로 트리를 열고 빛나는 칸을 하나 배운 뒤 닫으세요."),
        ("무기 진화", "보스를 쓰러뜨리면 무기가 진화합니다. 세 갈래 중 하나를 고르세요. (보통 판에서는 두 번 · 3장 보스는 각성, 되돌릴 수 없음)"),     // 1.1~ 3장 각성
        // 1.0.4~: 실전의 빨간 경고를 미리 익힘 (맞아도 체력은 줄지 않음)
        ("경고 피하기", "적의 기술은 바닥에 빨간 경고가 먼저 뜹니다. 안쪽이 다 차면 터지니, 그 전에 원 밖으로 피하세요! (세 번)"),
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
    bool stepDone, skipStep;
    GameObject stopButton, skipButton, confirm;
    RectTransform pointer;          // 단계마다 짚어 주는 HUD 테두리

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
            // 1.0.4~: [이 단계 건너뛰기]를 누르면 단계를 멈추고 다음으로 (창이 열려 시간이 멈춘 동안은 누를 수 없음)
            stepDone = skipStep = false;
            Coroutine co = StartCoroutine(RunStep(step));
            while (!stepDone && !skipStep) yield return null;
            if (!stepDone)
            {
                StopCoroutine(co);
                ClearFoes();
                check.text = "";
                continue;
            }
            yield return Cleared();
        }
        finished = true;
        Prefs.SetInt(DoneKey, 1);
        Prefs.Save();
        CloseConfirm();
        if (stopButton != null) stopButton.SetActive(false);
        if (skipButton != null) skipButton.SetActive(false);
        if (pointer != null) pointer.gameObject.SetActive(false);
        title.text = Loc.T("튜토리얼 완료!");
        // 1.0.4~: 튜토리얼에서 다루지 않은 상점 · 포털 · ESC 를 짧게 알려 주고, 자동으로 나가지 않고 버튼으로 고름
        body.text = Loc.T("기본 조작을 모두 익혔습니다! 판에서는 떠돌이 상점에 다가가 [{INTERACT}], 보스를 잡으면 열리는 포털로 다음 장, ESC 는 일시정지 · 설정입니다.");
        counter.text = "";
        bar.fillAmount = 1f;
        Hostile.Play("chime", 0.8f, 1.2f);
        Hostile.Play("levelup", 0.6f, 1f);
        RectTransform panel = (RectTransform)ui.transform;
        UIKit.MakeButton(panel, "바로 첫 판 시작", new Vector2(-170f, -120f), new Vector2(320f, 70f), () =>
        {
            quickStart = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }, 26f);
        UIKit.MakeButton(panel, "메인 메뉴로", new Vector2(170f, -120f), new Vector2(320f, 70f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }, 26f);
    }

    IEnumerator RunStep(int i)
    {
        yield return StepRoutine(i);
        stepDone = true;
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
                    // 1.0.4~: 앞 단계에서 다 쏴서 저절로 재장전 중이면 끝날 때까지 기다린 뒤, [재장전] 키로 직접 한 것만 인정
                    while (player.IsReloading) { SetProgress(0f); yield return null; }
                    player.NowBullet = Mathf.Min(player.NowBullet, Mathf.Max(1, player.MaxBullet / 2));
                    bool pressed = false;
                    while (true)
                    {
                        SetProgress(0f);
                        // 키를 누른 프레임에 플레이어가 먼저 재장전을 시작했을 수도 있으므로 재장전 중인지와 상관없이 기록
                        // (저절로 재장전 중에 누른 키는 아래 안쪽 기다림에서 보지 않으므로 세지 않음)
                        if (KeyBindings.Down(GameAction.Reload)) pressed = true;
                        if (player.IsReloading)
                        {
                            if (pressed) break;
                            // 다 쏴서 저절로 시작된 재장전: 끝나면 다시 반쯤 비워 두고 기다림
                            while (player.IsReloading) yield return null;
                            player.NowBullet = Mathf.Min(player.NowBullet, Mathf.Max(1, player.MaxBullet / 2));
                        }
                        yield return null;
                    }
                    while (player.IsReloading) yield return null;
                    break;
                }
            case 3:     // 필살기: 게이지를 채워 주고 적 넷 (조준할 적이 있어야 함)
                {
                    SpawnFoes(4);
                    if (gauge != null) gauge.AddSkillPoint(gauge.MaxSkillPoint);
                    // 1.0.4~: 한 명 이상 조준해 쏴야 통과 (살짝 눌렀다 떼면 게이지를 다시 채워 줌), 일제 사격이 끝난 뒤 정리
                    int aimed = 0;
                    while (gauge != null)
                    {
                        SetProgress(0f);
                        RefillGaugeIfIdle();
                        if (player.IsAiming) aimed = player.AimedCount;
                        if (!gauge.IsFull() && !player.IsAiming)
                        {
                            if (aimed > 0) break;
                            check.text = Loc.T("적을 조준한 뒤 떼세요");
                            check.color = Gold;
                            gauge.AddSkillPoint(gauge.MaxSkillPoint);
                        }
                        yield return null;
                    }
                    while (player.IsSkillUsing) yield return null;
                    check.text = "";
                    yield return new WaitForSeconds(0.4f);
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
            case 7:     // 1.0.4~ 경고 피하기: 발밑에 빨간 원 → 다 차기 전에 밖으로 (세 번 피하면 끝, 맞아도 체력은 바로 참)
                {
                    const float R = 2.2f, Warn = 1.3f;
                    int dodged = 0;
                    Color danger = new Color(1f, 0.25f, 0.2f, 0.9f);
                    while (dodged < 3)
                    {
                        SetProgress(dodged / 3f);
                        yield return new WaitForSeconds(0.6f);
                        Vector3 at = Hostile.ClampArena(player.transform.position);
                        Hostile.Circle(at, R, Warn, danger);
                        yield return new WaitForSeconds(Warn);
                        Fx.Spawn("fx_shock", at, R * 2.4f, new Color(1f, 0.45f, 0.35f), 20f);
                        if (Vector2.Distance(at, player.transform.position) > R + 0.4f)
                        {
                            dodged++;
                            check.text = "";
                        }
                        else
                        {
                            player.TryHit(1f);
                            check.text = Loc.T("맞았습니다! 다시 한 번");
                            check.color = new Color(1f, 0.5f, 0.45f);
                        }
                    }
                    check.text = "";
                    break;
                }
            case 8:     // 마무리: 진화한 무기로 다섯
                yield return Fight(5);
                break;
        }
    }

    // 단계를 마침: 초록 체크 · 소리 · 잠깐 쉼
    IEnumerator Cleared()
    {
        SetProgress(1f);
        check.text = "√ " + Loc.T("완료!");
        check.color = DoneColor;
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
        // 1.0.4~: 확인 창은 ESC 로도 닫힘 (일시정지 메뉴가 같이 열리지 않게)
        if (confirm != null && GameInput.EscDown) { SettingsUI.EscHandledFrame = Time.frameCount; CloseConfirm(); }
        // 창이 열려 시간이 멈춘 동안은 단계를 건너뛸 수 없음 (진화 · 카드 창이 반쯤 남지 않게)
        if (skipButton != null) skipButton.SetActive(Time.timeScale > 0f && confirm == null);
        // 1.0.5~ 패드: R3 로 이 단계 건너뛰기 (버튼 글자에도 [R3])
        if (skipButton != null && skipButton.activeSelf && GamePad.R3Down) skipStep = true;
        if (skipLabel != null && (skipLabelPad != GamePad.Active || skipLabelLang != Loc.Current))
        {
            skipLabelPad = GamePad.Active;
            skipLabelLang = Loc.Current;
            skipLabel.text = Loc.T("이 단계 건너뛰기") + (skipLabelPad ? " [R3]" : "");
        }
    }

    TMP_Text skipLabel;             // 1.0.5~ 「이 단계 건너뛰기」 글자 (패드면 [R3] 를 붙임, UIKit.Remember 대신 직접 갱신)
    bool skipLabelPad;
    Loc.Lang skipLabelLang = (Loc.Lang)(-1);

    // 1.0.4~: 단계마다 해당 HUD 를 금빛 테두리로 짚어 줌 (재장전 → 탄창, 필살기 → 게이지, 레벨업 → 배지, 영혼 트리 → 버튼)
    readonly Vector3[] corners = new Vector3[4];

    void LateUpdate()
    {
        if (pointer == null) return;
        RectTransform target = finished ? null : PointTarget();
        bool show = target != null && target.gameObject.activeInHierarchy;
        if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
        if (!show) return;
        Camera cam = CanvasCamera(target);
        RectTransform parent = (RectTransform)pointer.parent;
        Camera pcam = CanvasCamera(parent);
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, sp, pcam, out Vector2 lp);
            min = Vector2.Min(min, lp);
            max = Vector2.Max(max, lp);
        }
        float pad = 10f + 4f * Mathf.Sin(Time.unscaledTime * 6f);
        pointer.localPosition = (min + max) * 0.5f;
        pointer.sizeDelta = (max - min) + Vector2.one * pad * 2f;
        pointer.SetAsLastSibling();
    }

    static Camera CanvasCamera(Transform t)
    {
        Canvas c = t.GetComponentInParent<Canvas>();
        if (c == null) return null;
        c = c.rootCanvas;
        return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
    }

    RectTransform PointTarget()
    {
        switch (step)
        {
            case 2:
                {
                    ShowBullet sb = FindFirstObjectByType<ShowBullet>();
                    return sb != null && sb.TextInput != null ? sb.TextInput.rectTransform : null;
                }
            case 3: return gauge != null && gauge.gaugeImage != null ? gauge.gaugeImage.rectTransform : null;
            case 4: return shop != null && shop.PendingBadge != null ? shop.PendingBadge.transform as RectTransform : null;
            case 5: return sm != null && sm.UpgradeButton != null ? sm.UpgradeButton.transform as RectTransform : null;
        }
        return null;
    }

    void BuildPointer(Transform canvas)
    {
        pointer = UIKit.Rect("TutorialPointer", canvas, Vector2.zero, new Vector2(100f, 100f));
        Color c = new Color(1f, 0.85f, 0.35f, 0.95f);
        // 네 변 (속이 빈 테두리)
        (Vector2 min, Vector2 max, Vector2 size)[] edges =
        {
            (new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 5f)),
            (new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f)),
            (new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(5f, 0f)),
            (new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(5f, 0f)),
        };
        foreach (var e in edges)
        {
            RectTransform r = UIKit.Rect("Edge", pointer, Vector2.zero, e.size);
            r.anchorMin = e.min;
            r.anchorMax = e.max;
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = e.size;
            Image img = r.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
        }
        pointer.gameObject.SetActive(false);
    }

    // 1.0.4~: 그만두기는 한 번 더 묻기 (ESC 메뉴 「메인 메뉴로」와 같이)
    void AskQuit()
    {
        if (confirm != null) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        RectTransform win = UIKit.Modal(canvas.transform, "ConfirmQuitTutorial", new Vector2(780f, 340f), out confirm);
        SoulTreeUI.OnTop(confirm, 620);
        // 시간은 멈추지 않음 (튜토리얼 적은 아프지 않고, ESC 일시정지 메뉴와 멈춤 상태가 꼬이지 않게)
        UIKit.Text(win, "튜토리얼을 그만둘까요?", 38f, Gold, new Vector2(0f, 78f), new Vector2(720f, 60f));
        UIKit.Text(win, "메인 메뉴로 돌아갑니다. 다음에는 1단계부터 시작합니다.", 26f, Parch, new Vector2(0f, 20f), new Vector2(720f, 40f));
        UIKit.MakeButton(win, "메인 메뉴로", new Vector2(-150f, -90f), new Vector2(260f, 76f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }, 28f);
        UIKit.MakeButton(win, "취소", new Vector2(150f, -90f), new Vector2(260f, 76f), CloseConfirm, 28f);
    }

    void CloseConfirm()
    {
        if (confirm == null) return;
        Destroy(confirm);
        confirm = null;
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
        check = UIKit.Text(panel, "", 28f, DoneColor, new Vector2(390f, 46f), new Vector2(220f, 40f), TextAlignmentOptions.Right);
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

        // 1.0.4~: 바로 나가지 않고 확인 창 (AskQuit)
        Button stop = UIKit.MakeButton(canvas.transform, "튜토리얼 그만두기", Vector2.zero, new Vector2(300f, 60f), AskQuit, 24f);
        RectTransform sr = (RectTransform)stop.transform;
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(1f, 1f);
        sr.anchoredPosition = new Vector2(-30f, -110f);
        stopButton = stop.gameObject;
        // 1.0.4~: 아는 단계는 건너뛰기
        Button skip = UIKit.MakeButton(canvas.transform, "이 단계 건너뛰기", Vector2.zero, new Vector2(300f, 56f), () => { if (Time.timeScale > 0f) skipStep = true; }, 22f);
        RectTransform kr = (RectTransform)skip.transform;
        kr.anchorMin = kr.anchorMax = kr.pivot = new Vector2(1f, 1f);
        kr.anchoredPosition = new Vector2(-30f, -180f);
        skipButton = skip.gameObject;
        skipLabel = skip.GetComponentInChildren<TMP_Text>();
        if (skipLabel != null) UIKit.Forget(skipLabel);
        BuildPointer(canvas.transform);
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
