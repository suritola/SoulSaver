
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class Shop : MonoBehaviour
{
    public GameObject shopPanel;

    [Header("공격력 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput;
    public TextMeshProUGUI statTextInput;

    [Header("공속 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput2;
    public TextMeshProUGUI statTextInput2;

    [Header("재장전속도 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput3;
    public TextMeshProUGUI statTextInput3;

    [Header("탄창 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput4;
    public TextMeshProUGUI statTextInput4;

    [Header("이동속도 버튼 텍스트")]
    public TextMeshProUGUI priceTextInput5;
    public TextMeshProUGUI statTextInput5;

    public TextMeshProUGUI mycoins;

    [Header("코인")]
    public int coins;

    [Header("스텟")]
    public float damage = 1f;
    public float ShootSpeed = 0.8f;
    public float ReloadSpeed = 3f;
    public int MaxBullet = 6;
    public float showedSpeed;
    public float moveSpeed;

    [Header("가격")]
    public int damagePrice = 5;
    public int ShootSpeedPrice = 5;
    public int ReloadSpeedPrice = 5;
    public int MaxBulletPrice = 5;
    public int moveSpeedPrice = 5;

    public bool isShopOpen = false;

    public Coin coind;
    public PlayerController playerControllerd;

    [Header("게임 재시작")]
    public GameObject pause;
    public TextMeshProUGUI pauseText;


    void Start()
    {
        Debug.Log("Shop 시작");

        coind = FindFirstObjectByType<Coin>();
        playerControllerd = FindFirstObjectByType<PlayerController>();

        // 상점이 가진 능력치를 플레이어의 시작 값과 맞춤
        if (playerControllerd != null)
        {
            damage = playerControllerd.damage;
            ShootSpeed = playerControllerd.ShootSpeed;
            ReloadSpeed = playerControllerd.reloadTime;
            MaxBullet = playerControllerd.MaxBullet;
        }

        if (shopPanel != null) shopPanel.SetActive(false);
        else Debug.LogError("shopPanel이 연결되지 않았습니다!");
        BasicStatsOnly();
        BuildPotions();

        if (pause != null) pause.SetActive(false);

        UpdateShopText();
    }

    // ================================================================= 떠돌이 상점 제단
    [Header("상점 제단")]
    public int killsPerStall = 35;          // 일반 몹을 이만큼 잡을 때마다 제단이 나타남
    float killsForStall;

    void OnEnable() => EnermyController.Killed += OnEnemyKilled;
    void OnDisable() => EnermyController.Killed -= OnEnemyKilled;

    void OnEnemyKilled(Vector3 pos)
    {
        // 보스 전에는 처치 수가 늘어난 만큼 천천히 셈 (한 판에 나오는 상점 수 · 코인과 맞춤)
        EnemySpawner sp = Cache<EnemySpawner>.Get;
        killsForStall += sp != null && !sp.bossSpawned && !sp.bossCleared ? 1f / GameMode.KillStretch : 1f;
        if (killsForStall < killsPerStall || playerControllerd == null) return;
        killsForStall = 0;

        // 플레이어 근처, 벽이 아닌 곳
        Vector3 at = playerControllerd.transform.position;
        for (int i = 0; i < 12; i++)
        {
            Vector3 p = Hostile.ClampArena(playerControllerd.transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(5f, 8f)));
            if (!Hostile.WallNear(p, 1.5f)) { at = p; break; }
        }
        ShopStall.Spawn(this, at);
        // 1.0.4~: 「영구히 · 체력」 → 이번 판 동안 · 실제로 파는 것 (공격력 · 이동 속도 · 물약), 머무는 시간 10초 → 15초
        Hints.Show("shop", "떠돌이 상점에서는 코인으로 이번 판 동안 공격력 · 이동 속도를 올리고 물약을 살 수 있습니다. 가까이 가서 [{INTERACT}]");
        Notice.Big(Loc.T("떠돌이 상점이 나타났다!\n15초 안에 다가가 [{INTERACT}]"), NoticeIcon.Shop, 3f, NoticeLevel.Normal);
    }

    // 1.0.4~: 떠돌이 상점을 열지 못하고 놓쳤으면 다음 상점은 절반만 잡아도 나타남 (ShopStall 이 부름)
    public void StallMissed() => killsForStall = Mathf.Max(killsForStall, killsPerStall * 0.5f);

    // 떠돌이 상점을 열었을 때 (업적 등)
    public static event System.Action StallOpened;

    // 제단에 다가갔을 때 (열렸으면 true)
    public bool OpenFromStall()
    {
        if (isShopOpen || isPause || Time.timeScale != 1f) return false;
        ToggleShop();
        if (isShopOpen) StallOpened?.Invoke();
        return isShopOpen;
    }



    void Update()
    {
        // 특수 능력 화면이 열려 있으면 상점을 열지 않음
        bool specialOpen = StageManager.Instance != null && StageManager.Instance.IsMenuOpen;
        // 상점은 떠돌이 제단으로만 열림 (P는 닫기만)
        if (Input.GetKeyDown(KeyCode.P) && isShopOpen && !specialOpen) ToggleShop();
        if (brokeLabel != null && brokeLabel.gameObject.activeSelf && Time.unscaledTime > brokeUntil) brokeLabel.gameObject.SetActive(false);
    }


    [Header("가격 증가 / 최대치")]
    // 한 번에 확 강해지지 않도록 작게 자주 오르는 방식
    public float damageStep = 0.25f;            // 살 때마다 공격력 +0.25 (기본 1의 25%)
    public float damagePriceGrowth = 1.3f;      // 살 때마다 가격 x1.3
    public int shootSpeedPriceStep = 5;         // 살 때마다 가격 +5
    public float shootSpeedMultiplier = 0.94f;  // 발사 간격 x0.94 (6% 빨라짐)
    public float minShootSpeed = 0.22f;
    public float reloadPriceGrowth = 1.45f;
    public float reloadStep = 0.12f;
    public float minReloadTime = 0.9f;
    public float maxBulletPriceGrowth = 1.5f;
    public int maxBulletLimit = 14;
    public float moveSpeedPriceGrowth = 1.6f;
    public float moveSpeedMultiplier = 1.04f;
    public int maxMoveSpeedBuys = 5;

    int moveSpeedBuys = 0;

    // 영혼 트리 '흥정' 할인을 뺀 실제 가격
    // 1.0.1: 모든 상점 강화 가격 1.3배 (최고점은 그대로, 상점 성장을 늦춤) · 영혼 트리 「흥정」 할인은 그 위에
    const float GrowthPriceMul = 1.3f;
    static int Discounted(int price) => Mathf.CeilToInt(price * GrowthPriceMul * (SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.TreeShopMul : 1f));

    bool TryPay(int price)
    {
        price = Discounted(price);
        if (coind == null || playerControllerd == null) return false;
        if (coind.coins < price) { NotEnoughCoins(); return false; }      // 1.0.4~ 예전엔 아무 반응이 없어 고장인 줄 알았음

        coind.SubCoin(price);
        coins = coind.coins;
        return true;
    }

    bool ShootSpeedMaxed => ShootSpeed <= minShootSpeed + 0.001f;
    bool ReloadMaxed => ReloadSpeed <= minReloadTime + 0.001f;
    bool MaxBulletMaxed => MaxBullet >= maxBulletLimit;
    bool MoveSpeedMaxed => moveSpeedBuys >= maxMoveSpeedBuys;

    // =====================================
    // 공격력 업그레이드
    // =====================================

    public void OnPressB1()
    {
        using var log = new BuyLog(this, 1);     // 판 중간 저장: 코인이 줄면 기록
        if (!TryPay(damagePrice)) return;

        // 캐릭터 공격력 배율을 지키도록 플레이어 값에 바로 더함 (검사 150% · 도적 70% …)
        playerControllerd.damage += KitDamageStep;
        damage = playerControllerd.damage;
        damagePrice = Mathf.CeilToInt(damagePrice * damagePriceGrowth);
        UpdateShopText();
        Bought(buyAttack);
    }

    // =====================================
    // 공속 업그레이드
    // =====================================

    public void OnPressB2()
    {
        using var log = new BuyLog(this, 2);     // 판 중간 저장: 코인이 줄면 기록
        if (KitPress(2)) return;
        if (ShootSpeedMaxed || !TryPay(ShootSpeedPrice)) return;

        ShootSpeed = Mathf.Max(minShootSpeed, ShootSpeed * shootSpeedMultiplier);
        playerControllerd.ShootSpeed = ShootSpeed;
        ShootSpeedPrice += shootSpeedPriceStep;
        UpdateShopText();
    }

    // =====================================
    // 재장전 속도 업그레이드
    // =====================================

    public void OnPressB3()
    {
        using var log = new BuyLog(this, 3);     // 판 중간 저장: 코인이 줄면 기록
        if (KitPress(3)) return;
        if (ReloadMaxed || !TryPay(ReloadSpeedPrice)) return;

        ReloadSpeed = Mathf.Max(minReloadTime, ReloadSpeed - reloadStep);
        playerControllerd.reloadTime = ReloadSpeed;
        ReloadSpeedPrice = Mathf.CeilToInt(ReloadSpeedPrice * reloadPriceGrowth);
        UpdateShopText();
    }

    // =====================================
    // 탄창 업그레이드
    // =====================================

    public void OnPressB4()
    {
        using var log = new BuyLog(this, 4);     // 판 중간 저장: 코인이 줄면 기록
        if (KitPress(4)) return;
        if (MaxBulletMaxed || !TryPay(MaxBulletPrice)) return;

        MaxBullet++;
        playerControllerd.MaxBullet = MaxBullet;
        MaxBulletPrice = Mathf.CeilToInt(MaxBulletPrice * maxBulletPriceGrowth);
        UpdateShopText();
    }

    // =====================================
    // 이동속도 업그레이드
    // =====================================

    public void OnPressB5()
    {
        using var log = new BuyLog(this, 5);     // 판 중간 저장: 코인이 줄면 기록
        if (MoveSpeedMaxed || !TryPay(moveSpeedPrice)) return;

        moveSpeedBuys++;
        playerControllerd.speed *= moveSpeedMultiplier;
        moveSpeedPrice = Mathf.CeilToInt(moveSpeedPrice * moveSpeedPriceGrowth);
        UpdateShopText();
        Bought(buyMove);
    }

    // ================================================================= 판 중간 저장 (RunSave, 2.1.9~)
    // 상점 버튼을 눌러 코인이 줄었으면 (실제로 샀으면) 그 버튼 번호를 기록
    readonly struct BuyLog : IDisposable
    {
        readonly Shop shop;
        readonly int button, before;
        public BuyLog(Shop s, int b) { shop = s; button = b; before = s.coind != null ? s.coind.coins : 0; }
        public void Dispose() { if (shop.coind != null && shop.coind.coins < before) RunSave.Record("b" + button); }
    }

    // 기록한 구매를 다시 함 (값은 치르지 않음: 코인을 잠깐 넉넉히 채웠다가 되돌림)
    public void ReplayBuy(int button)
    {
        if (coind == null) coind = FindFirstObjectByType<Coin>();
        if (playerControllerd == null) playerControllerd = FindFirstObjectByType<PlayerController>();
        if (coind == null || playerControllerd == null) return;
        int keep = coind.coins;
        coind.coins = 1 << 28;
        switch (button)
        {
            case 1: OnPressB1(); break;
            case 2: OnPressB2(); break;
            case 3: OnPressB3(); break;
            case 4: OnPressB4(); break;
            case 5: OnPressB5(); break;
        }
        coind.coins = keep;
        coins = keep;
    }

    public bool isPause = false;
    void ToggleShop()
    {
        if (isShopOpen)
        {
            // 상점 닫기
            isShopOpen = false;

            if (shopPanel != null) shopPanel.SetActive(false);

            StartCoroutine(StartGameCountdown());
        }
        else
        {
            // 상점 열기 (다른 메뉴나 타겟팅 스킬 중에는 열지 않음)
            if (isPause || Time.timeScale != 1f) return;

            isShopOpen = true;

            if (coind != null) coins = coind.coins;

            if (shopPanel != null) shopPanel.SetActive(true);

            UpdateShopText();
            RefreshCloseLabel();

            Time.timeScale = 0f;
        }
    }


    // =====================================
    // 게임 재시작 카운트다운
    // =====================================

    public IEnumerator StartGameCountdown()
    {
        isPause = true;
        if (pause != null) pause.SetActive(true);


        Time.timeScale = 0f;

        for (int countdown = 1; countdown > 0; countdown--)
        {
            if (pauseText != null) pauseText.text = Loc.T("일시정지 ") + countdown + Loc.T("초");

            // Time.timeScale = 0이어도 시간이 흐름
            yield return new WaitForSecondsRealtime(1f);
        }

        if (pause != null)
        {
            pause.SetActive(false);
        }
        isPause = false;

        // 1.0.1: 일시정지 메뉴가 열려 있으면 지금 풀지 않고, 메뉴를 닫을 때 흐르게
        if (ESCmenu.IsOpen) ESCmenu.ResumeAt(1f);
        else Time.timeScale = 1f;
    }


    // =====================================
    // 상점 텍스트 갱신
    // =====================================

    // 1.0.4~: 코인이 모자라면 가격을 빨갛게
    string PriceText(int price, bool maxed) => maxed ? Loc.T("최대") : Loc.T("구매\n") + CoinPrice(Discounted(price));
    string CoinPrice(int discounted)
    {
        string s = discounted + Loc.T(" 코인");
        return coind != null && coind.coins < discounted ? "<color=#FF7A6E>" + s + "</color>" : s;
    }

    static string PerSecond(float interval) => (1f / interval).ToString("0.0");

    public void UpdateShopText()
    {
        if (coind == null || playerControllerd == null) return;

        coins = coind.coins;
        // 캐릭터 · 레벨업 카드가 바꾼 값을 따라감 (상점이 먼저 시작해도 어긋나지 않게)
        damage = playerControllerd.damage;
        ShootSpeed = playerControllerd.ShootSpeed;
        ReloadSpeed = playerControllerd.reloadTime;
        MaxBullet = playerControllerd.MaxBullet;
        moveSpeed = playerControllerd.speed;
        showedSpeed = 1f / ShootSpeed;
        if (mycoins != null) mycoins.text = Loc.T("코인 : ") + coins;


        // 공격력
        if (priceTextInput != null) priceTextInput.text = PriceText(damagePrice, false);
        if (statTextInput != null)
        {
            statTextInput.text = Loc.T(KitDamageLabel) + "\n" + damage.ToString("0.##") + " -> " + (damage + KitDamageStep).ToString("0.##");

            // 멀티샷 중이면 실제 한 발당 피해도 함께 표시
            int shots = playerControllerd.multiShot;
            if (shots > 1)
            {
                float rate = playerControllerd.MultiShotDamageRate(shots);
                statTextInput.text += "  (" + shots + Loc.T("발, 발당 ") + (damage * rate).ToString("0.##")
                    + " -> " + ((damage + KitDamageStep) * rate).ToString("0.##") + ")";
            }
        }

        // 공격 속도 (초당 발사 수)
        if (priceTextInput2 != null) priceTextInput2.text = PriceText(ShootSpeedPrice, ShootSpeedMaxed);
        if (statTextInput2 != null)
        {
            statTextInput2.text = ShootSpeedMaxed
                ? Loc.T("공격 속도\n초당 ") + PerSecond(ShootSpeed) + Loc.T("발 (최대)")
                : Loc.T("공격 속도 (초당)\n") + PerSecond(ShootSpeed) + " -> " + PerSecond(Mathf.Max(minShootSpeed, ShootSpeed * shootSpeedMultiplier)) + Loc.T("발");
        }

        // 재장전 속도
        if (priceTextInput3 != null) priceTextInput3.text = PriceText(ReloadSpeedPrice, ReloadMaxed);
        if (statTextInput3 != null)
        {
            statTextInput3.text = ReloadMaxed
                ? Loc.T("재장전 속도\n") + ReloadSpeed.ToString("0.0") + Loc.T("초 (최대)")
                : Loc.T("재장전 속도\n") + ReloadSpeed.ToString("0.0") + Loc.T("초 -> ") + Mathf.Max(minReloadTime, ReloadSpeed - reloadStep).ToString("0.0") + Loc.T("초");
        }

        // 탄창
        if (priceTextInput4 != null) priceTextInput4.text = PriceText(MaxBulletPrice, MaxBulletMaxed);
        if (statTextInput4 != null)
        {
            statTextInput4.text = MaxBulletMaxed
                ? Loc.T("최대 탄창\n") + MaxBullet + Loc.T("발 (최대)")
                : Loc.T("최대 탄창\n") + MaxBullet + " -> " + (MaxBullet + 1) + Loc.T("발");
        }

        // 이동 속도
        if (priceTextInput5 != null) priceTextInput5.text = PriceText(moveSpeedPrice, MoveSpeedMaxed);
        if (statTextInput5 != null)
        {
            statTextInput5.text = MoveSpeedMaxed
                ? Loc.T("이동 속도\n") + moveSpeed.ToString("0.0") + Loc.T(" (최대)")
                : Loc.T("이동 속도\n") + moveSpeed.ToString("0.0") + " -> " + (moveSpeed * moveSpeedMultiplier).ToString("0.0");
        }

        KitShopText();
        WindowText();
    }
}