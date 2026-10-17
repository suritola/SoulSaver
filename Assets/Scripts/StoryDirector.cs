using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 시작 연출 (검은 화면 → 플레이어가 위에서 떨어짐 → 짧은 이야기)과 엔딩 이야기
// Space로 대사 넘기기(쓰는 중이면 완성, 다 나왔으면 다음 대사), ESC · 건너뛰기 버튼으로 언제든 건너뜀
public class StoryDirector : MonoBehaviour
{
    public static bool Playing { get; private set; }
    public static bool EndingPlaying { get; private set; }
    // 배치 모드(자동 테스트)에서도 연출을 보고 싶을 때
    public static bool ForceInBatch;

    // 1.1.9~ 대사 속도 (설정 → 접근성): 보통 0.035 · 빠름 0.015 · 즉시 0 (편의성 2차 10)
    static float CharTime => GameSettings.DialogueSpeed switch { 1 => 0.015f, 2 => 0f, _ => 0.035f };
    const float LineHold = 1.7f;

    static readonly string[] IntroLines =
    {
        "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
        "마을에 남은 마지막 총잡이는 무너진 신전의 구멍으로 몸을 던졌다.",
        "리치 왕을 쓰러뜨리면 그 너머 지옥의 문도 열릴 것이다.",
        "탄창은 가득하다. 이제 세상을 구할 차례다.",
    };

    static readonly string[] EndlessLines =
    {
        "지옥 너머, 모든 괴물이 모여드는 불타는 사막.",
        "쓰러뜨린 왕들이 몇 번이고 다시 일어선다.",
        "끝은 없다. 얼마나 버틸 수 있을까?",
    };

    static readonly string[] EndingLines =
    {
        "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
        "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
        "총잡이는 마지막 탄피를 주워 모자 띠에 꽂았다.",
        "살아남은 사람들은 그를 이렇게 불렀다.",
        "영혼을 지켜 세상을 구한 자 — Soul Saver.",
        "하지만 지평선 너머, 붉게 타오르는 사막에서 무언가 꿈틀거리고 있었다...",
    };


    // 2.1.1~ 장 순서 (1장 초원 · 킹 슬라임) 의 시작 이야기. 체험판처럼 예전 순서(1장 묘역)면 아래 IntroFor
    static readonly string[] MeadowIntroLines =
    {
        "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
        "마을에 남은 마지막 총잡이는 홀로 녹아내린 초원으로 걸어 나갔다.",
        "킹 슬라임을 쓰러뜨리면, 땅 밑의 진짜 적이 모습을 드러낼 것이다.",
        "탄창은 가득하다. 이제 세상을 구할 차례다.",
    };

    static string[] MeadowIntroFor(CharacterId id) => id switch
    {
        CharacterId.Swordsman => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "떠돌이 기사는 녹슨 장검 한 자루를 등에 메고 산성 늪 앞에 섰다.",
            "총알은 떨어져도, 칼날은 떨어지지 않는다.",
            "기사는 망설임 없이 늪을 건너기 시작했다.",
        },
        CharacterId.Rogue => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "그림자 칼날이라 불리는 도적은 초원 아래 잠든 보물 이야기를 들었다.",
            "망자든 악마든, 등을 보인 놈부터 쓰러진다.",
            "도적은 소리 없이 풀숲 속으로 몸을 숨겼다.",
        },
        CharacterId.Archer => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "숲의 사냥꾼은 짐승들이 녹아내린 초원을 피해 달아나는 것을 보았다.",
            "흔적을 거슬러 오르자, 거대한 슬라임이 초원 한가운데 버티고 있었다.",
            "화살통은 가득하다. 이번 사냥감은 킹 슬라임이다.",
        },
        CharacterId.Alchemist => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "미친 학자는 오히려 기뻤다. 초원만 한 슬라임이라니, 이렇게 좋은 재료가 또 있을까.",
            "플라스크를 허리춤에 가득 채우고, 학자는 산성 늪으로 성큼 걸어 들어갔다.",
            "“자, 실험을 시작하지.”",
        },
        CharacterId.Cartographer => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "길 잃은 측량사는 지도에서 초원 한 칸이 통째로 지워진 것을 발견했다.",
            "지워진 땅은 다시 그리면 된다. 깃펜 끝에 잉크를 듬뿍 찍었다.",
            "“첫 번째 선은 저 슬라임의 둘레로 하지.”",
        },
        CharacterId.Clockmaker => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "마을 시계탑의 바늘이 멈췄다. 시계공은 그것이 고장이 아니라 경고라는 것을 알았다.",
            "멈춘 시간을 다시 돌리려면, 원인을 부숴야 한다.",
            "시계공은 톱니바퀴 한 주머니를 챙겨 초원으로 향했다.",
        },
        CharacterId.Postman => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "저승 배달부의 가방에 수취인이 「킹 슬라임」인 편지가 한 통 들어 있었다.",
            "보낸 이는 초원에서 녹아 사라진 사람들. 내용은 짧았다. 「돌려줘.」",
            "배달부는 모자를 고쳐 쓰고 초원으로 걸어 나갔다. 편지는 반드시 전해진다.",
        },
        CharacterId.Ghost => new[]
        {
            "킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 것들까지 꿈틀대기 시작했다.",
            "오래전 이 초원에서 죽은 유령은 아직 떠나지 못하고 있었다.",
            "떠나지 못한 이유를 이제야 알았다. 지켜야 할 것이 남아 있었다.",
            "유령은 차가운 손을 쥐었다 폈다. 이번에는 끝까지 지켜 낼 것이다.",
        },
        _ => MeadowIntroLines,
    };

    // 캐릭터마다 다른 시작 · 엔딩 이야기 (거너는 위의 기본 이야기)
    static string[] IntroFor(CharacterId id) => id switch
    {
        CharacterId.Swordsman => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "떠돌이 기사는 녹슨 장검 한 자루를 등에 메고 무너진 신전 앞에 섰다.",
            "총알은 떨어져도, 칼날은 떨어지지 않는다.",
            "기사는 망설임 없이 어둠 속으로 뛰어내렸다.",
        },
        CharacterId.Rogue => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "그림자 칼날이라 불리는 도적은 신전 지하에 잠든 보물 이야기를 들었다.",
            "망자든 악마든, 등을 보인 놈부터 쓰러진다.",
            "도적은 소리 없이 구멍 아래로 몸을 날렸다.",
        },
        CharacterId.Archer => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "숲의 사냥꾼은 죽은 짐승들이 숲을 떠나 신전으로 향하는 것을 보았다.",
            "흔적을 따라가자, 무너진 신전의 구멍이 나타났다.",
            "화살통은 가득하다. 이번 사냥감은 리치 왕이다.",
        },
        CharacterId.Alchemist => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "미친 학자는 오히려 기뻤다. 되살아난 시체라니, 이렇게 좋은 재료가 또 있을까.",
            "플라스크를 허리춤에 가득 채우고, 학자는 신전의 구멍으로 뛰어들었다.",
            "“자, 실험을 시작하지.”",
        },
        CharacterId.Cartographer => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "길 잃은 측량사는 어떤 지도에도 없는 구멍이 신전 바닥에 뚫린 것을 보았다.",
            "지도에 없는 곳이라면, 지금 그리면 된다.",
            "측량사는 깃펜을 쥐고 어둠 속으로 첫 선을 그었다.",
        },
        CharacterId.Clockmaker => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "마을 시계탑의 바늘이 멈췄다. 시계공은 그것이 고장이 아니라 경고라는 것을 알았다.",
            "죽은 자가 걸어 다니는 것은, 누군가 시간을 거꾸로 감았기 때문이다.",
            "시계공은 톱니바퀴 한 주머니를 챙겨 신전의 구멍으로 내려갔다.",
        },
        CharacterId.Postman => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "저승 배달부의 가방에 수취인이 「리치 왕」인 편지가 한 통 들어 있었다.",
            "보낸 이는 묘역에 잠들어 있던 모든 영혼. 내용은 짧았다. 「이제 그만 재워 줘.」",
            "배달부는 모자를 고쳐 쓰고 신전의 구멍으로 내려갔다. 편지는 반드시 전해진다.",
        },
        CharacterId.Ghost => new[]
        {
            "리치 왕이 지하 묘역을 깨우자, 죽은 자들이 땅 위로 기어 나왔다.",
            "그중 하나는 리치 왕의 부름을 거역했다. 묘역에서 가장 오래된 유령이었다.",
            "죽은 자를 깨우는 왕이라면, 죽은 자가 재워야 한다.",
            "유령은 소리 없이 신전의 구멍으로 스며들었다.",
        },
        _ => IntroLines,
    };

    static string[] EndingFor(CharacterId id) => id switch
    {
        CharacterId.Swordsman => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "기사는 이 빠진 장검을 닦아 칼집에 꽂았다.",
            "사람들은 총 한 자루 없이 세상을 구한 기사를 신기한 듯 바라보았다.",
            "“총이 아니어도 괜찮다. 지킬 수만 있다면.”",
            "하지만 지평선 너머, 붉게 타오르는 사막에서 무언가 꿈틀거리고 있었다...",
        },
        CharacterId.Rogue => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "도적은 리치 왕의 왕관을 슬쩍 품에 넣었다.",
            "영웅의 이름을 물었을 때, 도적은 이미 그림자 속으로 사라진 뒤였다.",
            "다음 날, 왕궁의 보물 창고가 텅 비었다는 소문이 돌았다.",
            "하지만 지평선 너머, 붉게 타오르는 사막에서 무언가 꿈틀거리고 있었다...",
        },
        CharacterId.Archer => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "사냥꾼은 마지막 화살을 거두어 화살통에 꽂았다.",
            "숲에는 다시 새소리가 돌아왔다.",
            "사람들은 숲의 사냥꾼에게 깊이 고개를 숙였다.",
            "하지만 지평선 너머, 붉게 타오르는 사막에서 무언가 꿈틀거리고 있었다...",
        },
        CharacterId.Alchemist => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "학자는 킹 슬라임의 점액을 플라스크에 담으며 킬킬 웃었다.",
            "“이걸로 논문 세 편은 쓰겠군.”",
            "세상은 구해졌다. 대부분은 우연이었지만.",
            "하지만 지평선 너머, 붉게 타오르는 사막에서 무언가 꿈틀거리고 있었다...",
        },
        CharacterId.Cartographer => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "측량사는 마지막 빈칸에 「심연」이라고 적고 지도를 접었다.",
            "세상에서 가장 정확한 지도가 완성된 날이었다.",
            "“이제 길을 잃는 사람은 없겠지.”",
            "하지만 지도 끝 붉게 칠해진 사막에, 아직 그리지 못한 무언가가 꿈틀거리고 있었다...",
        },
        CharacterId.Clockmaker => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "멈췄던 시계탑의 바늘이 다시 째깍거리기 시작했다.",
            "시계공은 바늘을 1분 앞으로 돌려 두었다. 늘 조금 일찍 준비하는 편이 좋으니까.",
            "“다음은 언제 멈출지, 이제 나는 알지.”",
            "하지만 지평선 너머, 붉게 타오르는 사막에서 무언가 꿈틀거리고 있었다...",
        },
        CharacterId.Postman => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "배달부의 가방이 텅 비었다. 모든 편지가 제 주인에게 닿았다.",
            "떠오르는 영혼들이 하나씩 배달부에게 손을 흔들었다. 답장 대신이었다.",
            "“배달 완료.”",
            "하지만 가방 바닥에서, 붉은 사막의 소인이 찍힌 편지 한 통이 바스락거렸다...",
        },
        CharacterId.Ghost => new[]
        {
            "거울의 군주가 산산이 부서지자, 심연에 갇혀 있던 영혼들이 빛이 되어 떠올랐다.",
            "리치 왕의 저주도, 지옥 군주의 불길도, 킹 슬라임의 산성도 모두 사라졌다.",
            "떠오르는 영혼들 사이에서, 유령의 몸도 조금씩 빛으로 풀려 갔다.",
            "지켜야 할 것을 끝까지 지켰다. 이제 떠나도 된다.",
            "“…고마웠어.”",
            "하지만 마지막 빛이 사라지기 전, 붉은 사막 쪽에서 낯익은 냉기가 불어왔다...",
        },
        _ => EndingLines,
    };

    Canvas canvas;
    Image black;
    Image shade;
    TMP_Text body;
    TMP_Text title;
    GameObject skipButton;
    bool skip;

    public static bool ShouldSkipAll => GameInput.Auto || GameInput.TrailerRunning || (Application.isBatchMode && !ForceInBatch);

    // ================================================================= 시작
    public static IEnumerator Intro(bool endless)
    {
        yield return null;                    // 트레일러 촬영이 켜질 때까지 한 프레임 기다림
        if (ShouldSkipAll) yield break;
        StoryDirector d = Create();
        yield return d.RunIntro(endless);
        Destroy(d.gameObject);
    }

    IEnumerator RunIntro(bool endless)
    {
        Playing = true;
        GameInput.ResetAttackLock();        // 1.1.9~ 공격 고정은 이야기 연출이 시작되면 풀림
        // 1.1~ 같은 캐릭터로 같은 이야기를 이미 봤으면 (두 번째 판부터) 이야기는 건너뛰고 떨어지는 연출만 짧게
        string seenKey = "intro.seen." + (endless ? "e" : Chapters.IsClassic ? "c" : "m") + "." + (int)CharacterData.Selected;
        bool seen = Prefs.GetInt(seenKey, 0) == 1;
        // 게임 시작 때 뜨는 능력 카드 창은 연출이 끝날 때까지 숨김
        LevelShop shop = Cache<LevelShop>.Get;
        GameObject cards = shop != null ? shop.LvshopPanel : null;
        bool cardsWere = cards != null && cards.activeSelf;
        if (cards != null) cards.SetActive(false);
        PlayerController player = Hostile.Player;
        SpriteRenderer[] hidden = player != null ? player.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0];
        bool[] wasOn = new bool[hidden.Length];
        for (int i = 0; i < hidden.Length; i++) { wasOn[i] = hidden[i].enabled; hidden[i].enabled = false; }

        SetAlpha(black, 1f);
        SetAlpha(shade, 0f);

        // 위에서 떨어지는 가짜 플레이어 + 커지는 그림자
        GameObject faller = null, shadow = null;
        Vector3 land = player != null ? player.transform.position : Vector3.zero;
        SpriteRenderer main = player != null ? player.GetComponent<SpriteRenderer>() : null;
        if (main != null)
        {
            faller = new GameObject("IntroFaller");
            SpriteRenderer fr = faller.AddComponent<SpriteRenderer>();
            fr.sprite = main.sprite;
            fr.sortingLayerID = main.sortingLayerID;
            fr.sortingOrder = main.sortingOrder + 5;
            faller.transform.localScale = player.transform.lossyScale;
            shadow = new GameObject("IntroShadow");
            SpriteRenderer sh = shadow.AddComponent<SpriteRenderer>();
            sh.sprite = SpecialAbilities.GlowSprite;
            sh.color = new Color(0f, 0f, 0f, 0.5f);
            sh.sortingLayerID = main.sortingLayerID;
            sh.sortingOrder = main.sortingOrder - 1;
            shadow.transform.position = land + new Vector3(0f, -0.9f, 0f);
        }

        // 1) 검은 화면이 걷히며 떨어짐 (게임 시간은 흐르되 적 생성 · 조작은 멈춤)
        // 1.1.9~ 설정 「빠른 연출」이면 절반 (편의성 3차 7)
        const float fallFrom = 13f;
        float fadeTime = 0.9f * GameSettings.FxTime, fallTime = 0.75f * GameSettings.FxTime;
        for (float t = 0f; t < fadeTime + fallTime && !skip; t += Time.unscaledDeltaTime)
        {
            SetAlpha(black, 1f - Mathf.Clamp01(t / fadeTime));
            float k = Mathf.Clamp01((t - fadeTime * 0.4f) / fallTime);
            if (faller != null) faller.transform.position = land + new Vector3(0f, fallFrom * (1f - k * k), 0f);
            if (shadow != null) shadow.transform.localScale = new Vector3(1.6f, 0.5f, 1f) * Mathf.Lerp(0.2f, 1f, k);
            yield return null;
        }
        SetAlpha(black, 0f);
        if (faller != null) Destroy(faller);
        if (shadow != null) Destroy(shadow);
        for (int i = 0; i < hidden.Length; i++) if (hidden[i] != null) hidden[i].enabled = wasOn[i];

        // 착지: 먼지 · 흔들림 · 쿵
        if (!skip)
        {
            Fx.Spawn("fx_smoke", land + new Vector3(0f, -0.7f, 0f), 3.2f, new Color(0.85f, 0.8f, 0.72f, 0.9f), 18f);
            Fx.Spawn("fx_shock", land + new Vector3(0f, -0.8f, 0f), 4f, new Color(1f, 0.9f, 0.7f, 0.7f), 20f);
            Hostile.Shake(0.45f);
            Hostile.Play("thump", 1f, 0.8f);
            for (float t = 0f; t < 0.6f && !skip; t += Time.unscaledDeltaTime) yield return null;
        }

        // 2) 이야기 (시간을 멈추고 글자가 한 줄씩)
        bool watched = false;           // 1.1~ 이야기를 [건너뛰기] 없이 끝까지 봤을 때만 「본 것」으로 기록 (건너뛰었으면 다음 판에 다시 나옴)
        if (!skip && !seen)
        {
            float before = Time.timeScale;
            Time.timeScale = 0f;
            yield return Fade(shade, 0f, 0.78f, 0.4f);
            yield return Lines(endless ? EndlessLines : Chapters.IsClassic ? IntroFor(CharacterData.Selected) : MeadowIntroFor(CharacterData.Selected));
            watched = !skip;
            yield return Fade(shade, GetAlpha(shade), 0f, 0.4f);
            Time.timeScale = before;         // 시작 능력 카드 창이 떠 있으면 그대로 멈춰 있음
        }
        if (cards != null && cardsWere) cards.SetActive(true);
        if (!seen && watched) { Prefs.SetInt(seenKey, 1); Prefs.Save(); }
        Playing = false;
    }

    // ================================================================= 엔딩
    // lines: 캐릭터 엔딩 대사 (무한 모드 클리어는 대사 없이 바로 마무리 · 1.0.2)
    public static IEnumerator Ending(Difficulty cleared, string opened, bool lines = true)
    {
        StoryDirector d = Create();
        yield return d.RunEnding(cleared, opened, lines);
    }

    IEnumerator RunEnding(Difficulty cleared, string opened, bool lines)
    {
        Playing = true;
        EndingPlaying = true;
        Time.timeScale = 0f;
        GameInput.ResetAttackLock();
        // 1.1.9~ 도감 「이야기」 탭: 이야기 모드 엔딩을 본 것으로 (대사를 건너뛰어도 클리어는 했으므로 열어 줌 · 무한 모드 클리어는 대사가 없음)
        if (lines) { Prefs.SetInt("ending.seen." + (int)CharacterData.Selected, 1); Prefs.Save(); }
        // 1.1~ 최종 클리어 보너스 (결과 화면의 획득 포인트에 들어가게 씬을 바꾸기 전에)
        // 일일 도전은 같은 날 몇 번이고 다시 깰 수 있어 포인트 벌이 길이 되므로 주지 않음 (이야기 · 무한 모드는 판마다 한 번)
        int bonusBefore = Point.RunBonus;
        if (!DailyChallenge.Active) Point.Bonus(Point.FinalBonus);
        bool gotBonus = Point.RunBonus > bonusBefore;
        SetAlpha(black, 0f);
        SetAlpha(shade, 0f);
        yield return Fade(black, 0f, 1f, 1.5f);
        if (!skip && lines) yield return Lines(EndingFor(CharacterData.Selected));

        // 마무리: 클리어 · 새로 열린 난이도 · 인사
        skip = false;
        title.text = GameMode.Name(cleared) + " " + Loc.T("클리어!");
        string sub = opened != null ? Loc.T("새 난이도 해금: ") + Loc.T(opened) + "\n\n" : "";
        string bonus = gotBonus ? Loc.T("result.finalbonus").Replace("{0}", Point.FinalBonus.ToString("N0")) + "\n\n" : "";
        body.text = sub + bonus + Loc.T("플레이해 주셔서 고맙습니다!");
        yield return Fade(title, 0f, 1f, 0.6f);
        advance = false;
        for (float t = 0f; t < 4f * GameSettings.FxTime && !skip && !advance; t += Time.unscaledDeltaTime) yield return null;

        Playing = false;                // timeScale 은 SceneFlow 가 막을 내린 뒤 새 씬에서 1 로 (막이 내리는 동안 게임이 풀리지 않게)
        EndingPlaying = false;
        // 1.0.4~: 엔딩 뒤 바로 메인 메뉴가 아니라 결과 화면(기록 · 획득 포인트 · 다음 해금)을 「클리어!」로 보여 줌
        RunStats.MarkCleared();
        RunStats.CaptureForResult();        // 1.1.9~ 결과 화면 「빌드 자세히 보기」 스냅샷 (떠나는 순간의 빌드)
        SceneFlow.Go("GameOver");
    }

    // ================================================================= 공통
    IEnumerator Lines(string[] lines)
    {
        foreach (string line in lines)
        {
            string text = Loc.T(line);
            body.text = text;
            body.maxVisibleCharacters = 0;
            advance = false;
            // 1.1.9~ 대사 속도 「즉시」: 한 줄을 한 번에 (소리는 줄마다 한 번)
            if (CharTime <= 0f)
            {
                if (voice == null) Hostile.Play("pew", 0.12f, 2.2f);
                else Hostile.Play(voice, 0.55f, voicePitch);
            }
            // 한 글자씩 (Space를 누르면 이 대사를 바로 끝까지)
            for (int n = CharTime <= 0f ? text.Length + 1 : 0; n <= text.Length && !skip && !advance; n++)
            {
                body.maxVisibleCharacters = n;
                if (n < text.Length && text[n] != ' ')
                {
                    if (voice == null) { if (n % 3 == 0) Hostile.Play("pew", 0.12f, 2.2f); }
                    else if (n % 2 == 0) Hostile.Play(voice, 0.55f, voicePitch * Random.Range(0.88f, 1.12f));
                }
                float until = Time.unscaledTime + CharTime;
                while (Time.unscaledTime < until && !skip && !advance) yield return null;
            }
            body.maxVisibleCharacters = 99999;
            advance = false;
            // 다 나온 대사에서 Space를 누르면 다음 대사로
            for (float t = 0f; t < LineHold && !skip && !advance; t += Time.unscaledDeltaTime) yield return null;
            advance = false;
            if (skip) yield break;
        }
        body.text = "";
    }

    // Space = 대사 넘기기 (쓰는 중이면 완성, 다 나왔으면 다음 대사), ESC · 건너뛰기 버튼 = 이야기 전체 건너뛰기
    bool advance;
    // 보스 대사를 말할 때의 목소리 (SpecialFeedback 효과음 이름, null = 타자 소리)
    string voice;
    float voicePitch = 1f;

    // ================================================================= 보스 등장
    // 게임이 멈추고 → 카메라가 보스 쪽으로 부드럽게 이동 → 위아래 검은 띠와 함께 보스가 말함 → 돌아옴
    static readonly (string name, string title, string[] lines, string voice, float pitch)[] Bosses =
    {
        ("리치 왕", "지하 묘역의 주인", new[] { "감히 내 잠든 묘역을 깨우다니...", "네 영혼도 내 망자의 군대에 더해 주마!" }, "voice_lich", 0.9f),
        ("지옥의 군주", "불타는 지옥의 왕", new[] { "하찮은 인간이 지옥의 문턱을 넘었군.", "불길 속에서 재가 되어라!" }, "voice_demon", 0.8f),
        ("킹 슬라임", "초원의 폭군", new[] { "뿌요... 이 초원은 이 몸의 식탁이다!", "납작하게 짓눌러 주마, 뿌요요옹!" }, "voice_slime", 1.1f),
        ("거울의 군주", "심연 끝에 선 그림자", new[] { "...나는 네가 쓰러뜨린 모든 것의 메아리.", "네가 쌓아 온 그 힘으로, 너를 부숴 주마." }, "voice_lich", 0.7f),
    };

    // 1.2~ 보스 재회 한 줄 (설계 3-2 ③): 이 보스를 전에 만났으면 등장 대사 뒤에 「또 너냐」류 한 줄 · 5번 넘게 쓰러뜨렸으면 다른 한 줄
    //   만난 적 = boss.met.<종류> (등장 연출이 한 번이라도 나온 뒤 · 건너뛰어도) — 「처음만」 설정의 boss.intro.seen.<종류>(대사를 끝까지 봄)과 따로 · 쓰러뜨린 수 = boss.down.<종류> (RunCountersHost) · 글 LocTable.Herald 의 boss.again(5).<보스>
    const string MetKey = "boss.met.";
    static readonly string[] ReunionIds = { "lich", "demon", "slime", "mirror" };

    static string[] ReunionLines(int kind, string[] lines)
    {
        if (kind < 0 || kind >= ReunionIds.Length || (Prefs.GetInt(MetKey + kind, 0) != 1 && Prefs.GetInt(BossSeenKey(kind), 0) != 1)) return lines;
        string key = (Prefs.GetInt("boss.down." + kind, 0) >= 5 ? "boss.again5." : "boss.again.") + ReunionIds[kind];
        if (!Loc.Has(key)) return lines;
        string[] all = new string[lines.Length + 1];
        lines.CopyTo(all, 0);
        all[lines.Length] = key;
        return all;
    }

    // 한 판(무한 모드 포함)에서 보스마다 처음 나올 때 한 번만
    static readonly System.Collections.Generic.HashSet<int> introduced = new System.Collections.Generic.HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ResetIntros()
    {
        SceneManager.sceneLoaded += (s, m) => introduced.Clear();
    }

    internal static string BossSeenKey(int kind) => "boss.intro.seen." + kind;

    // 1.1.9~ 도감 「이야기」 탭 (편의성 2차 6): 표를 그대로 돌려줌
    internal static string[] IntroLinesFor(CharacterId id, bool classic) => classic ? IntroFor(id) : MeadowIntroFor(id);
    internal static string[] EndlessIntroLines => EndlessLines;
    internal static string[] EndingLinesFor(CharacterId id) => EndingFor(id);
    internal static int BossCount => Bosses.Length;
    internal static (string name, string title, string[] lines) BossLines(int kind) => (Bosses[kind].name, Bosses[kind].title, Bosses[kind].lines);
    internal static string IntroSeenKey(bool endless, bool classic, CharacterId id) => "intro.seen." + (endless ? "e" : classic ? "c" : "m") + "." + (int)id;
    // 1.2.3~ 무한 모드 이어하기: 되살린 보스의 인트로를 다시 틀지 않음 (씬이 바뀔 때 비워지므로 복원 직전에 넣음)
    public static void MarkIntroduced(int kind) => introduced.Add(kind);

    public static void PlayBossIntro(Transform boss, int kind)
    {
        if (boss == null || kind < 0 || kind >= Bosses.Length || ShouldSkipAll || Playing) return;
        if (!introduced.Add(kind)) return;
        StoryDirector d = Create();
        d.StartCoroutine(d.RunBossIntro(boss, kind));
    }

    IEnumerator RunBossIntro(Transform boss, int kind)
    {
        Playing = true;
        GameInput.ResetAttackLock();
        var info = Bosses[kind];
        // 1.1.9~ 보스 등장 대사 「처음만」 (편의성 2차 5): 끝까지 본 보스는 다음부터 띠 · 포효 · 이름표만 짧게 (첫 번째는 늘 전체 · 설정 「항상」이면 늘 전체)
        string seenKey = BossSeenKey(kind);
        bool brief = Prefs.GetInt(seenKey, 0) == 1 && !GameSettings.BossIntroAlways;
        if (brief && skipButton != null) skipButton.SetActive(false);
        if (brief && skipHint != null) skipHint.gameObject.SetActive(false);
        float barTime = brief ? 0.5f : 0.7f;
        float before = Time.timeScale;
        Time.timeScale = 0f;
        SetAlpha(black, 0f);
        SetAlpha(shade, 0f);

        // 위아래 검은 띠
        RectTransform top = Bar(true), bottom = Bar(false);
        SpecialFeedback fx = SpecialAbilities.SharedFx;
        if (fx != null) fx.HoldCamera(boss.position);

        // 이름표와 대사는 아래 띠 안에 — 1.1~ 화면 아래에 붙여 두고 띠를 150 → 210 으로 (대사가 띠 경계에 걸쳐 게임 화면 위에 떴고, 16:10 에선 띠 밖이었음)
        //      대사 폭 1500 → 1200: 오른쪽 아래 건너뛰기 버튼 · 안내와 안 겹치게
        foreach (TMP_Text tx in new[] { title, body }) tx.rectTransform.anchorMin = tx.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        body.rectTransform.sizeDelta = new Vector2(1200f, 100f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 165f);
        body.rectTransform.anchoredPosition = new Vector2(0f, 64f);
        body.fontSize = 34f;
        title.text = Loc.T(info.name) + "  <size=60%><color=#d8c8a8>" + Loc.T(info.title) + "</color></size>";
        SetAlpha(title, 0f);

        for (float t = 0f; t < barTime && !skip; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / barTime);
            top.sizeDelta = bottom.sizeDelta = new Vector2(0f, BossBarH * k);
            if (fx != null && boss != null) fx.HoldCamera(boss.position);
            yield return null;
        }
        top.sizeDelta = bottom.sizeDelta = new Vector2(0f, BossBarH);
        Hostile.Play("roar", 0.8f, kind == 2 ? 1.4f : kind == 1 ? 0.7f : 1f);
        yield return Fade(title, 0f, 1f, 0.3f);

        voice = info.voice;
        voicePitch = info.pitch;
        if (brief)
        {
            // 1.2~ 이미 본 보스: 이름표 + 재회 한 줄만 짧게 (보스 재회 · 재회 글이 없으면 이름표만)
            string[] reunion = ReunionLines(kind, new string[0]);
            if (reunion.Length > 0) { if (!skip) yield return Lines(reunion); }
            else for (float t = 0f; t < 0.9f && !skip; t += Time.unscaledDeltaTime) yield return null;
        }
        else
        {
            if (!skip) yield return Lines(ReunionLines(kind, info.lines));      // 1.2~ 보스 재회 한 줄
            if (!skip) { Prefs.SetInt(seenKey, 1); Prefs.Save(); }      // 건너뛴 사람은 다음에 다시 봄 (시작 이야기와 같은 규칙)
        }
        voice = null;
        Prefs.SetInt(MetKey + kind, 1);      // 재회용 「만난 적」만 · 대사 본 기록(seenKey)은 끝까지 봤을 때만 위에서

        // 돌아오기
        if (fx != null) fx.ReleaseCamera();
        skip = false;
        SetAlpha(title, 0f);
        body.text = "";
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / 0.4f;
            top.sizeDelta = bottom.sizeDelta = new Vector2(0f, BossBarH * k);
            yield return null;
        }
        Time.timeScale = before;        // 필살기 조준(느린 시간) 중이었으면 그대로
        Playing = false;
        Destroy(gameObject);
    }

    const float BossBarH = 210f;       // 1.1~ 보스 등장 위아래 띠 높이 (150 → 210, 이름 · 대사가 띠 안에)

    RectTransform Bar(bool top)
    {
        GameObject go = new GameObject(top ? "BarTop" : "BarBottom", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(canvas.transform, false);
        r.SetSiblingIndex(2);                              // 검은 화면 위, 글자 아래
        r.anchorMin = new Vector2(0f, top ? 1f : 0f);
        r.anchorMax = new Vector2(1f, top ? 1f : 0f);
        r.pivot = new Vector2(0.5f, top ? 1f : 0f);
        r.sizeDelta = new Vector2(0f, 0f);
        r.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
        go.GetComponent<Image>().raycastTarget = false;
        return r;
    }

    // 1.0.4~: ESC 는 두 번 눌러야 건너뜀 (멈추려고 누른 ESC 한 번에 이야기가 통째로 날아갔음)
    //   대사 넘기기는 Space 와 함께 [상호작용] 키 · 좌클릭으로도 (확정 키를 바꿔도 따라감)
    const string SkipHintKo = "[{INTERACT}] · 클릭: 대사 넘기기   ESC 두 번: 건너뛰기";
    const string SkipArmKo = "ESC 를 한 번 더 누르면 이야기를 건너뜁니다";
    // 1.0.5~ 패드: [A] 넘기기 · B(또는 Menu) 두 번 건너뛰기
    const string SkipHintPadKo = "[A] 대사 넘기기   B 두 번: 건너뛰기";
    const string SkipArmPadKo = "B 를 한 번 더 누르면 이야기를 건너뜁니다";
    static string SkipHint => GamePad.Active ? SkipHintPadKo : SkipHintKo;
    static string SkipArm => GamePad.Active ? SkipArmPadKo : SkipArmKo;
    TMP_Text skipHint;
    float escArmedUntil = -1f;

    void Update()
    {
        if (GameInput.EscDown)
        {
            if (Time.unscaledTime < escArmedUntil) skip = true;
            else
            {
                escArmedUntil = Time.unscaledTime + 2f;
                if (skipHint != null) { skipHint.text = Loc.T(SkipArm); skipHint.color = new Color(1f, 0.88f, 0.4f); }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space) || KeyBindings.Down(GameAction.Interact) || Input.GetMouseButtonDown(0) || GamePad.ADown) advance = true;
        // 2초 안에 다시 누르지 않으면 안내를 되돌림
        if (skipHint != null && escArmedUntil > 0f && Time.unscaledTime >= escArmedUntil)
        {
            escArmedUntil = -1f;
            skipHint.text = Loc.T(SkipHint);
            skipHint.color = new Color(0.78f, 0.74f, 0.68f);
        }
    }

    void OnDestroy()
    {
        if (!EndingPlaying) Playing = false;
    }

    static StoryDirector Create()
    {
        GameObject go = new GameObject("StoryDirector", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        StoryDirector d = go.AddComponent<StoryDirector>();
        d.canvas = go.GetComponent<Canvas>();
        d.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        d.canvas.sortingOrder = 31000;     // 게임 UI(능력 카드 등)보다 항상 위
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        UiScaler.Fit(cs);               // 설정 「UI 크기」 (2.1.9)
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;     // 16:10 · 21:9 에서도 1920×1080 영역이 다 보이게
        UIKit.EnsureStyle();

        d.shade = FullImage(go.transform, "Shade", new Color(0.02f, 0.01f, 0.03f, 0f));
        d.black = FullImage(go.transform, "Black", new Color(0f, 0f, 0f, 0f));

        d.title = UIKit.Text(go.transform, "", 64f, new Color(0.96f, 0.83f, 0.47f, 0f), new Vector2(0f, 120f), new Vector2(1400f, 100f));
        d.body = UIKit.Text(go.transform, "", 40f, new Color(0.95f, 0.91f, 0.84f), new Vector2(0f, -40f), new Vector2(1500f, 240f));
        d.body.enableAutoSizing = false;
        d.body.fontSize = 40f;
        d.body.enableWordWrapping = true;

        // 오른쪽 아래: 건너뛰기 버튼 + 키 안내
        Button b = UIKit.MakeButton(go.transform, "건너뛰기", Vector2.zero, new Vector2(230f, 64f), () => d.skip = true, 26f);
        RectTransform br = (RectTransform)b.transform;
        br.anchorMin = br.anchorMax = new Vector2(1f, 0f);
        br.anchoredPosition = new Vector2(-150f, 70f);
        // 1.0.4~: 안내 글자 18 → 22, [상호작용] 키 · 클릭 · ESC 두 번 안내 (ESC 를 누르면 「한 번 더」 안내로 바뀜)
        d.skipHint = UIKit.Text(go.transform, SkipHintKo, 22f, new Color(0.78f, 0.74f, 0.68f), Vector2.zero, new Vector2(620f, 34f), TextAlignmentOptions.Right);
        UIKit.Forget(d.skipHint);
        d.skipHint.text = Loc.T(SkipHint);
        d.skipHint.enableAutoSizing = true;
        d.skipHint.fontSizeMin = 14f;
        d.skipHint.fontSizeMax = 22f;
        RectTransform hint = d.skipHint.rectTransform;
        hint.anchorMin = hint.anchorMax = new Vector2(1f, 0f);
        hint.pivot = new Vector2(1f, 0.5f);
        hint.anchoredPosition = new Vector2(-35f, 122f);
        d.skipButton = b.gameObject;
        return d;
    }

    static Image FullImage(Transform parent, string name, Color c)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = true;       // 연출 중에는 뒤쪽 클릭을 막음
        return img;
    }

    IEnumerator Fade(Graphic g, float from, float to, float time)
    {
        for (float t = 0f; t < time && !skip; t += Time.unscaledDeltaTime)
        {
            SetAlpha(g, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetAlpha(g, to);
    }

    static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    static float GetAlpha(Graphic g) => g.color.a;
}
