# 스팀 업적 등록표

Steamworks → **앱 관리 → 스탯 및 업적 → 업적**에서 아래 **API 이름**을 그대로 써서 52개를 만듭니다. (2.0.8에 스킬 진화 · 보스 결계 6개, 2.1.1에 4장 영혼의 심연 2개, 2.0.0 대규모 업데이트에 사막 비밀 해금 5개 · 영혼 릴레이 4개 · 역류 3개 추가)
API 이름이 다르면 게임에서 풀리지 않습니다. (코드: `Assets/Scripts/SteamManager.cs`의 `SteamAchievements`)

- 아이콘: `docs/steam/art/achievements/` — 달성 `<API>.jpg`, 미달성 `<API>_locked.jpg` (256×256). 1.8.6에 추가한 6개는 `tools/steam/make_achievement_icons.py` 로 만든 PNG를 JPG로 변환 (Steamworks는 JPG 권장)
- 대부분 **숨김 아님**으로 두면 됩니다. (`ACH_CLEAR`만 숨김으로 해도 좋음) 단 사막 비밀 해금 5개(`UNLOCK_*`)는 **숨김 권장** — 이름 · 설명이 비밀의 존재를 알려 주므로 달성 전엔 가려 둡니다. 릴레이 · 역류 7개는 숨김 아님.
- 2.0.0 새 업적 12개의 아이콘 24장도 `make_achievement_icons.py` 로 만듦.
- `ACH_KILLS_3000_TOTAL`의 누적 처치 수는 이 PC에 저장됩니다 (PlayerPrefs `stats.totalKills`).
- 다 만든 뒤 **게시(Publish)**를 눌러야 적용됩니다.

| API 이름 | 한국어 | English | 日本語 | 中文 |
|---|---|---|---|---|
| `ACH_FIRST_ULT` | **첫 필살기** — 필살기를 처음 사용했다 | **First Ultimate** — Use an ultimate for the first time | **初めての必殺技** — 初めて必殺技を使った | **首次必杀** — 第一次使用必杀技 |
| `ACH_ENTER_HELL` | **지옥의 문** — 불타는 지옥에 들어섰다 | **Gates of Hell** — Enter the Burning Hell | **地獄の門** — 燃える地獄に足を踏み入れた | **地狱之门** — 进入燃烧地狱 |
| `ACH_ENTER_MEADOW` | **바깥 공기** — 초원에 다다랐다 | **Fresh Air** — Reach the Meadow | **外の空気** — 草原にたどり着いた | **新鲜空气** — 抵达草原 |
| `ACH_MIDBOSS` | **덩치 큰 녀석** — 중간 보스를 처치했다 | **Big One Down** — Defeat a mid-boss | **大物退治** — 中ボスを倒した | **大家伙** — 击败精英首领 |
| `ACH_EVOLVE` | **진화** — 능력을 처음 진화시켰다 | **Evolution** — Evolve an ability for the first time | **進化** — 初めて能力を進化させた | **进化** — 首次进化能力 |
| `ACH_LEVEL_10` | **숙련된 총잡이** — 한 판에서 레벨 10에 도달했다 | **Seasoned Gunner** — Reach level 10 in one run | **熟練ガンナー** — 1回のプレイでレベル10に到達 | **老练枪手** — 单局达到10级 |
| `ACH_KILLS_500` | **학살자** — 한 판에서 적 500마리를 처치했다 | **Slayer** — Defeat 500 enemies in one run | **殲滅者** — 1回のプレイで敵を500体倒した | **屠戮者** — 单局击败500个敌人 |
| `ACH_BOSS_LICH` | **왕의 몰락** — 리치 왕을 쓰러뜨렸다 | **Fall of the King** — Defeat the Lich King | **王の失墜** — リッチ王を倒した | **王之陨落** — 击败巫妖王 |
| `ACH_BOSS_DEMON` | **지옥을 식히다** — 지옥의 군주를 쓰러뜨렸다 | **Hell Freezes Over** — Defeat the Demon Lord | **地獄を鎮めて** — 地獄の君主を倒した | **冷却地狱** — 击败地狱领主 |
| `ACH_CLEAR` | **Soul Saver** — 거울의 군주를 쓰러뜨리고 게임을 클리어했다 | **Soul Saver** — Defeat the Mirror Sovereign and clear the game | **Soul Saver** — 鏡の君主を倒してゲームをクリア | **Soul Saver** — 击败镜之君主，通关游戏 |
| `ACH_FIRST_DEATH` | **다시 일어나라** — 처음으로 쓰러졌다 | **Get Back Up** — Fall in battle for the first time | **立ち上がれ** — 初めて倒れた | **重新站起来** — 第一次倒下 |
| `ACH_SHOPPER` | **단골 손님** — 떠돌이 상점을 처음 열었다 | **Regular Customer** — Open the wandering shop | **常連客** — さすらいのショップを初めて開いた | **老主顾** — 第一次打开流浪商店 |
| `ACH_FULL_SKILLS` | **운명을 쥐다** — 한 판에서 운명 가지 칸 3개를 배웠다 | **Hold Your Fate** — Learn 3 Fate nodes in one run | **運命を握る** — 1回のプレイで運命の枝を3マス習得した | **执掌命运** — 单局学习3个命运节点 |
| `ACH_ARSENAL` | **완성된 무기** — 한 판에서 무기를 두 번 진화시켰다 | **Perfected Weapon** — Evolve your weapon twice in one run | **完成された武器** — 1回のプレイで武器を2回進化させた | **完美武器** — 单局将武器进化两次 |
| `ACH_EVOLVE_3` | **완전체** — 한 판에서 능력 3개를 진화시켰다 | **Final Form** — Evolve 3 abilities in one run | **完全体** — 1回のプレイで能力を3つ進化させた | **完全体** — 单局进化3项能力 |
| `ACH_ULT_30` | **필살기 중독** — 한 판에서 필살기를 30번 사용했다 | **Ultimate Addict** — Use 30 ultimates in one run | **必殺技中毒** — 1回のプレイで必殺技を30回使った | **必杀成瘾** — 单局使用30次必杀技 |
| `ACH_LEVEL_15` | **베테랑** — 한 판에서 레벨 15에 도달했다 | **Veteran** — Reach level 15 in one run | **ベテラン** — 1回のプレイでレベル15に到達 | **老兵** — 单局达到15级 |
| `ACH_KILLS_3000_TOTAL` | **전설의 사냥꾼** — 누적 적 3000마리를 처치했다 | **Legendary Hunter** — Defeat 3,000 enemies in total | **伝説の狩人** — 累計で敵を3000体倒した | **传奇猎手** — 累计击败3000个敌人 |
| `ACH_NO_HIT_BOSS` | **완벽한 결투** — 피해를 받지 않고 보스를 쓰러뜨렸다 | **Flawless Duel** — Defeat a boss without taking damage | **完璧な決闘** — ダメージを受けずにボスを倒した | **完美对决** — 无伤击败首领 |
| `ACH_CLOSE_CALL` | **구사일생** — 체력 10% 이하로 보스를 쓰러뜨렸다 | **Close Call** — Defeat a boss with 10% HP or less | **九死に一生** — 体力10%以下でボスを倒した | **九死一生** — 以10%以下的生命值击败首领 |
| `ACH_UNLOCK_DIFFICULTY` | **새로운 도전** — 쉬움을 클리어해 보통 · 어려움을 열었다 | **New Challenge** — Clear Easy to unlock Normal and Hard | **新たな挑戦** — イージーをクリアしてノーマル · ハードを解放した | **新的挑战** — 通关简单难度，解锁普通和困难 |
| `ACH_CLEAR_NORMAL` | **한 걸음 더** — 보통 난이도를 클리어했다 | **One Step Further** — Clear the game on Normal | **さらに一歩** — ノーマルをクリアした | **更进一步** — 通关普通难度 |
| `ACH_CLEAR_HARD` | **총잡이의 전설** — 어려움 난이도를 클리어했다 | **Legend of the Gunslinger** — Clear the game on Hard | **ガンマンの伝説** — ハードをクリアした | **枪手传说** — 通关困难难度 |
| `ACH_ENDLESS_10` | **사막의 방랑자** — 무한 모드에서 10분 동안 살아남았다 | **Desert Wanderer** — Survive 10 minutes in Endless | **砂漠の放浪者** — エンドレスで10分間生き延びた | **沙漠流浪者** — 在无尽模式中存活10分钟 |
| `ACH_ENDLESS_20` | **사막의 주인** — 무한 모드에서 20분 동안 살아남았다 | **Lord of the Desert** — Survive 20 minutes in Endless | **砂漠の主** — エンドレスで20分間生き延びた | **沙漠之主** — 在无尽模式中存活20分钟 |
| `ACH_ENDLESS_BOSSES` | **왕들의 무덤** — 무한 모드 한 판에서 보스 5마리를 쓰러뜨렸다 | **Graveyard of Kings** — Defeat 5 bosses in one Endless run | **王たちの墓場** — エンドレス1回でボスを5体倒した | **王者之墓** — 单局无尽模式击败5个首领 |
| `ACH_WEAPON_EVOLVE` | **새로운 무기** — 무기를 처음 진화시켰다 | **New Weapon** — Evolve your weapon for the first time | **新たな武器** — 初めて武器を進化させた | **新武器** — 首次进化武器 |
| `ACH_TREE_20` | **뿌리 내리기** — 한 판에서 영혼 트리 칸 20개를 배웠다 | **Taking Root** — Learn 20 Soul Tree nodes in one run | **根を張る** — 1回のプレイで魂のツリーを20マス習得した | **扎根** — 单局学习20个灵魂树节点 |
| `ACH_TREE_40` | **세계수** — 한 판에서 영혼 트리 칸 40개를 배웠다 | **World Tree** — Learn 40 Soul Tree nodes in one run | **世界樹** — 1回のプレイで魂のツリーを40マス習得した | **世界树** — 单局学习40个灵魂树节点 |
| `ACH_REROLL` | **운명 비틀기** — 레벨업 카드를 다시 뽑았다 | **Twist of Fate** — Reroll your level-up cards | **運命のいたずら** — レベルアップカードを引き直した | **命运转折** — 重抽升级卡牌 |
| `ACH_SKIN` | **새 옷** — 스킨 상점에서 스킨을 처음 샀다 | **New Look** — Buy your first skin in the Skin Shop | **新しい装い** — スキンショップで初めてスキンを買った | **新装扮** — 首次在皮肤商店购买皮肤 |
| `ACH_SKIN_LEGEND` | **전설의 풍모** — 전설 등급 스킨을 샀다 | **Legendary Style** — Buy a Legendary skin | **伝説の風格** — レジェンドスキンを買った | **传奇风范** — 购买传说皮肤 |
| `ACH_SKILL_EVOLVE` | **하나가 된 힘** — 스킬을 처음으로 진화시켰다 | **United Power** — Evolve a skill for the first time | **一つになった力** — 初めてスキルを進化させた | **合一之力** — 首次进化技能 |
| `ACH_SKILL_EVOLVE_3` | **융합의 달인** — 한 판에서 스킬을 3번 진화시켰다 | **Master of Fusion** — Evolve 3 skills in one run | **融合の達人** — 1回のプレイでスキルを3回進化させた | **融合大师** — 单局进化3次技能 |
| `ACH_SKILL_EVOLVE_15` | **진화 수집가** — 서로 다른 스킬 진화를 15종 이루었다 | **Evolution Collector** — Achieve 15 different skill evolutions | **進化コレクター** — 異なるスキル進化を15種達成した | **进化收藏家** — 达成15种不同的技能进化 |
| `ACH_BARRIER_SURVIVE` | **결계를 버티다** — 보스의 결계를 끝까지 버텨 냈다 | **Endure the Barrier** — Survive a boss barrier to the end | **結界を耐えて** — ボスの結界を最後まで耐え抜いた | **熬过结界** — 坚持到首领结界结束 |
| `ACH_BARRIER_NOHIT` | **흠집 하나 없이** — 결계 안에서 한 번도 맞지 않고 버텼다 | **Not a Scratch** — Survive a boss barrier without being hit | **傷ひとつなく** — 結界の中で一度も被弾せずに耐え抜いた | **毫发无伤** — 在结界中一次都没被击中 |
| `ACH_BARRIER_BREAK` | **결계 파괴자** — 결계가 펼쳐진 동안 보스를 쓰러뜨렸다 | **Barrier Breaker** — Defeat a boss while its barrier is up | **結界破り** — 結界が展開している間にボスを倒した | **结界破坏者** — 在结界展开时击败首领 |
| `ACH_ENTER_ABYSS` | **심연으로** — 영혼의 심연에 발을 들였다 | **Into the Abyss** — Enter the Abyss of Souls | **深淵へ** — 魂の深淵に足を踏み入れた | **坠入深渊** — 踏入灵魂深渊 |
| `ACH_MIRROR_QUICK` | **거울을 깨뜨리다** — 거울의 군주가 결계를 펼치기 전에 쓰러뜨렸다 | **Shattered Reflection** — Defeat the Mirror Sovereign before it raises its barrier | **鏡を砕く** — 鏡の君主が結界を展開する前に倒した | **击碎镜像** — 在镜之君主展开结界前将其击败 |
| `UNLOCK_WRESTLER` | **씨름 한판** — 사막에서 씨름꾼을 찾았다 | **Best of Three** — Found the Wrestler in the desert | **一本勝負** — 砂漠で相撲取りを見つけた | **摔跤一局** — 在沙漠中找到了摔跤手 |
| `UNLOCK_FOREMAN` | **안전 제일** — 사막에서 공사 반장을 찾았다 | **Safety First** — Found the Foreman in the desert | **安全第一** — 砂漠で現場監督を見つけた | **安全第一** — 在沙漠中找到了工头 |
| `UNLOCK_PUPPETEER` | **보이지 않는 실** — 사막에서 인형사를 찾았다 | **Unseen Strings** — Found the Puppeteer in the desert | **見えない糸** — 砂漠で人形使いを見つけた | **看不见的线** — 在沙漠中找到了傀儡师 |
| `UNLOCK_UNDERTAKER` | **마지막 배웅** — 사막에서 장의사를 찾았다 | **Final Farewell** — Found the Undertaker in the desert | **最後の見送り** — 砂漠で葬儀屋を見つけた | **最后的送行** — 在沙漠中找到了殡葬师 |
| `UNLOCK_PHOTOGRAPHER` | **찰칵** — 사막에서 사진사를 찾았다 | **Say Cheese** — Found the Photographer in the desert | **はい、チーズ** — 砂漠で写真家を見つけた | **茄子** — 在沙漠中找到了摄影师 |
| `ACH_RELAY_FIRST` | **바통 터치** — 영혼 릴레이에서 10분 넘게 버텼다 | **Pass the Baton** — Survive over 10 minutes in Soul Relay | **バトンタッチ** — 魂のリレーで10分以上生き延びた | **交接棒** — 在灵魂接力中存活超过10分钟 |
| `ACH_RELAY_DEV` | **책임자 나오세요** — 영혼 릴레이에서 개발자를 쓰러뜨렸다 | **Take It Up With the Dev** — Defeat the Developer in Soul Relay | **責任者を出せ** — 魂のリレーで開発者を倒した | **叫负责人出来** — 在灵魂接力中击败开发者 |
| `ACH_RELAY_ALLBOSS` | **각자의 몫** — 영혼 릴레이에서 세 주자가 모두 보스를 쓰러뜨렸다 | **Everyone Pulls Their Weight** — Have all three runners defeat a boss in one Soul Relay run | **三者三様** — 魂のリレーで3人のランナー全員がボスを倒した | **各尽其责** — 在灵魂接力中三名接力者都击败了首领 |
| `ACH_RELAY_FIRSTHERO` | **끝까지 내가** — 영혼 릴레이에서 첫 주자 혼자 15분까지 버텼다 | **Anchor of Steel** — Last 15 minutes in Soul Relay with the first runner alone | **最後まで俺が** — 魂のリレーで最初のランナーだけで15分まで耐え抜いた | **我跑到底** — 在灵魂接力中仅靠第一位接力者坚持到15分钟 |
| `ACH_REFLUX_CLEAR` | **거슬러 오르다** — 역류를 클리어했다 | **Against the Current** — Clear Reflux | **流れに逆らって** — 逆流をクリアした | **逆流而上** — 通关逆流 |
| `ACH_REFLUX_FAST` | **감기 버튼** — 8분 안에 역류를 클리어했다 | **Fast Rewind** — Clear Reflux in under 8 minutes | **早送り巻き戻し** — 8分以内に逆流をクリアした | **快速倒带** — 在8分钟内通关逆流 |
| `ACH_REFLUX_NAKED` | **초심으로** — 맨몸이 된 뒤 처음의 슬라임을 한 대도 맞지 않고 쓰러뜨렸다 | **Back to Basics** — Defeat the very first slime without taking a hit after losing everything | **初心に帰る** — すべてを失った後、最初のスライムを一度も被弾せずに倒した | **回归初心** — 失去一切后，不受伤击败最初的史莱姆 |

## 풀리는 조건 (코드 기준)

| API 이름 | 조건 |
|---|---|
| `ACH_FIRST_ULT` | 우클릭 필살기 발동 (조준형 · 즉발형 모두) |
| `ACH_ENTER_HELL` / `ACH_ENTER_MEADOW` | 스테이지 2 / 3 시작 |
| `ACH_MIDBOSS` | 중간 보스 처치 |
| `ACH_EVOLVE` | 능력 진화 (영혼 트리의 패시브 진화, 거너 무기 2차 진화) |
| `ACH_LEVEL_10` | 플레이어 레벨 10 |
| `ACH_KILLS_500` | 한 판(게임 씬을 새로 시작한 뒤) 처치 수 500 |
| `ACH_BOSS_LICH` / `ACH_BOSS_DEMON` / `ACH_CLEAR` | 1장 / 2장 / 4장(2.1.1~, 예전엔 3장) 보스 처치 |
| `ACH_FIRST_DEATH` | 게임 오버 화면에 처음 도달 |
| `ACH_SHOPPER` | 떠돌이 상점 제단에서 상점을 엶 |
| `ACH_FULL_SKILLS` / `ACH_ARSENAL` | 한 판에서 운명 가지 칸 3개 / 무기 2차 진화 (1.8.6에 조건 변경: 예전 조건인 스킬 3개 · 특수 무기 2개는 1.8.2에 스킬이 빠지며 달성할 수 없게 됨. API 이름은 그대로) |
| `ACH_EVOLVE_3` / `ACH_ULT_30` | 한 판에서 진화 3회 / 필살기 30회 |
| `ACH_LEVEL_15` | 플레이어 레벨 15 |
| `ACH_KILLS_3000_TOTAL` | 여러 판 누적 처치 수 3000 |
| `ACH_NO_HIT_BOSS` | 스테이지 보스가 나온 뒤 쓰러질 때까지 체력이 한 번도 줄지 않음 |
| `ACH_CLOSE_CALL` | 스테이지 보스를 쓰러뜨린 순간 체력이 최대의 10% 이하 |
| `ACH_UNLOCK_DIFFICULTY` / `ACH_CLEAR_NORMAL` / `ACH_CLEAR_HARD` | 쉬움 / 보통 / 어려움에서 마지막 보스(2.1.1~ 4장 거울의 군주) 처치 |
| `ACH_ENDLESS_10` / `ACH_ENDLESS_20` | 무한 모드 한 판에서 10분 / 20분 생존 (일시정지 시간은 빼고) |
| `ACH_ENDLESS_BOSSES` | 무한 모드 한 판에서 보스 5마리 처치 |
| `ACH_WEAPON_EVOLVE` | 보스를 쓰러뜨리고 무기 1차 진화 |
| `ACH_TREE_20` / `ACH_TREE_40` | 한 판에서 영혼 트리 칸 20 / 40개 배움 (모든 가지 합) |
| `ACH_REROLL` | 레벨업 창에서 [R] 다시 뽑기 (운명의 실) |
| `ACH_SKIN` / `ACH_SKIN_LEGEND` | 스킨을 처음 삼 / 전설 스킨을 삼 (예전에 산 스킨도 다음 실행 때 인정) |
| `ACH_SKILL_EVOLVE` / `ACH_SKILL_EVOLVE_3` | 스킬 진화 1회 / 한 판에서 스킬 진화 3회 (2.0.8) |
| `ACH_SKILL_EVOLVE_15` | 서로 다른 스킬 진화(캐릭터별) 15종, 이 PC에 누적 (PlayerPrefs `stats.skillEvos`) |
| `ACH_BARRIER_SURVIVE` / `ACH_BARRIER_NOHIT` | 보스 결계가 끝날 때까지 버팀 / 그 동안 피해를 한 번도 받지 않음 |
| `ACH_BARRIER_BREAK` | 결계가 펼쳐진 동안 그 보스를 쓰러뜨림 |
| `ACH_ENTER_ABYSS` | 4장 영혼의 심연에 들어섬 (2.1.1, 무한 모드 제외) |
| `ACH_MIRROR_QUICK` | 거울의 군주가 결계 「거울의 방」을 한 번도 펼치지 않은 채 쓰러뜨림 (2.1.1) |
| `UNLOCK_WRESTLER` | 사막의 비밀 하나를 풀어 씨름꾼 해금 (`DesertSecrets` 황소 비밀, 처음 풀 때만) |
| `UNLOCK_FOREMAN` | 사막의 비밀 하나를 풀어 공사 반장 해금 (표지판 비밀) |
| `UNLOCK_PUPPETEER` | 사막의 비밀 하나를 풀어 인형사 해금 (더미 비밀) |
| `UNLOCK_UNDERTAKER` | 사막의 비밀 하나를 풀어 장의사 해금 (묘비 비밀) |
| `UNLOCK_PHOTOGRAPHER` | 사막의 비밀 하나를 풀어 사진사 해금 (군중 비밀) |
| `ACH_RELAY_FIRST` | 영혼 릴레이에서 판 시간 10분(600초) 도달 |
| `ACH_RELAY_DEV` | 영혼 릴레이에서 개발자 처치 |
| `ACH_RELAY_ALLBOSS` | 영혼 릴레이 한 판에서 세 주자가 각자 보스를 한 번씩 처치 (이어하기 포함) |
| `ACH_RELAY_FIRSTHERO` | 영혼 릴레이에서 첫 주자가 교대 없이 15분(최후의 시간) 도달 |
| `ACH_REFLUX_CLEAR` | 역류 클리어 |
| `ACH_REFLUX_FAST` | 역류를 480초(8분) 미만에 클리어 |
| `ACH_REFLUX_NAKED` | 「역류의 끝」 뒤 맨몸으로 처음의 슬라임을 피해 없이 처치하며 클리어 |

## Steamworks 현지화 파일

`docs/steam/achievement-loc/*.vdf` — Steamworks → 도전 과제 현지화에서 업로드한 파일 (영어 · 한국어 · 일본어 · 중국어 간체).
키 `NEW_ACHIEVEMENT_1_<번호>_NAME/DESC`의 번호는 위 표의 순서(0부터)와 같습니다. 도전 과제를 새로 만들면 번호가 바뀌므로 먼저 "All Languages"를 받아서 확인하세요.

### 2.0.0 새 업적 12개 — 토큰 번호와 만들 순서
번호는 Steamworks 에서 업적을 만든 순서(0부터)로 정해집니다. 기존 40개(0~39)를 만든 뒤 **아래 순서대로** 12개를 이어서 만들면 번호가 vdf 와 맞습니다.

| 번호 | API 이름 |
|---|---|
| 40 | `UNLOCK_WRESTLER` |
| 41 | `UNLOCK_FOREMAN` |
| 42 | `UNLOCK_PUPPETEER` |
| 43 | `UNLOCK_UNDERTAKER` |
| 44 | `UNLOCK_PHOTOGRAPHER` |
| 45 | `ACH_RELAY_FIRST` |
| 46 | `ACH_RELAY_DEV` |
| 47 | `ACH_RELAY_ALLBOSS` |
| 48 | `ACH_RELAY_FIRSTHERO` |
| 49 | `ACH_REFLUX_CLEAR` |
| 50 | `ACH_REFLUX_FAST` |
| 51 | `ACH_REFLUX_NAKED` |
