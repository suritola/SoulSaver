# 스팀 상점 페이지 (Soul Saver)

Steamworks → **상점 페이지 편집**에 들어가는 글입니다. 언어별 탭에 각각 넣습니다. 본문은 Steam BBCode.
이미지는 `docs/steam/art/` (크기는 [README](README.md#이미지-규격) 참고).

> 게임 이름은 v3.2부터 **Soul Saver** 입니다. (예전 이름 Gun Saver)
> 1.7.3부터 제품 이름(productName)과 실행 파일 이름도 **`Soul Saver.exe`** 입니다. 옛 저장 데이터는 첫 실행 때 자동으로 옮겨지고(`SaveMigration.cs`), Steamworks 실행 옵션도 `Soul Saver.exe`로 바꿔야 합니다.
> 빈틈 · 기억 빌드 기준으로 고쳤습니다(`docs/review/2026-10-18-차별점-구현.md` 4절): 한 줄을 「자동 조준이 없는 서바이버」로, 태그 · 컨트롤러 정정, 빈틈 · 기억 소개, 영웅 9명을 모두 꺼냄. **짧은 설명의 빈틈 · 기억 두 문장과 본문 「보스의 빈틈 · 기억하는 보스」 칸은 빈틈 · 기억 빌드를 올리는 날 넣습니다.** 그 전에 먼저 바꿀 때는 그 두 문장 대신 「내 빌드를 흉내 내는 거울의 군주」 문장(`docs/review/2026-10-17-차별점-전략.md` 4-A ②)을 씁니다.
> 스크린샷 순서: 지금 바로 `07_archer_demon_lord_barrier`(4개 언어 모두)를 맨 앞으로, `01_gunner_catacombs_horde` 는 뒤로. 빈틈 · 기억 빌드 촬영본이 나오면 ① `01_aim_barrier` ② 유령 빙의 또는 시계공 되감기 ③ 궁수 「완벽!」 ④ 거울의 군주 되쏘기 ⑤ 진화 3택 ⑥ 영웅 9명 ⑦ 개발자 ⑧ 카드 · 트리 · 스킨 순서.
> 1.0.7 기준으로 고쳤습니다: 거너 우클릭 이름 「일제 사격」, 장 중반 습격 · 보물 상자 · 보스 보상 · 저주 제단 · 최후의 보루 · 영웅 숙련 소개 추가.
> 2.0.6 기준으로 고쳤습니다: 영웅별 고유 스킬 50종 · 스킬 진화 37종 · 보스 필살기 「결계 침식」 · 최대 레벨 50 · 레벨업 건너뛰기 · 모든 우클릭 스킬 조준. 스크린샷 12장 · 트레일러는 `tools/trailer/record.ps1` 로 다시 찍음.
> 1.1.2 기준으로 고쳤습니다(대중성 조사 `docs/marketing/reports/서바이버류 게임 대중화 전략.md` 의 「지금 바로」): 짧은 설명을 차별점(보스 결계 1:1 → 쓰러뜨리면 그 자리에서 무기 진화) 앞세워 다시 씀, 태그 맨 앞에 Valve 공식 장르 태그 **Bullet Heaven**(2026-05 신설), 가벼운 손님을 밀어내는 Difficult 는 뺌, 본문 「그 밖에」에 방송 · 수익 창출 자유 한 줄.
> 1.8.6 기준으로 다시 썼습니다: 무기 진화 · 영혼 트리(운명 가지) · 스킨 상점 · 도전 과제 32개. 예전 글의 "특수 능력 52종 · 상점 무기 강화"는 1.7.8에 없어진 방식이라 뺐습니다.

## 기본 정보

| 항목 | 값 |
|---|---|
| 이름 | Soul Saver |
| 개발사 · 배급사 | 박민근 |
| 장르 | 액션, 인디, 로그라이트 |
| 추천 태그 (앞에서부터 중요한 순) | **보이는 5개**: Bullet Heaven, Top-Down Shooter, Action Roguelike, Twin Stick Shooter, Pixel Graphics · **6번째부터**: Roguelite, Dark Fantasy, Survival, Singleplayer, 2D, Controller, Shoot 'Em Up, Arcade, Fantasy, Indie, Action, Replay Value · **넣지 않음**: PvE(멀티 용어) · Bullet Hell(경고 뒤 발동 게임이라 거짓 기대) · Difficult · Boss Rush(그 모드가 생기기 전까지). 플레이어 투표가 가중치를 바꾸므로 2주 뒤 앞 5개를 다시 확인 |
| 지원 언어 (인터페이스 · 자막) | 한국어, 영어, 일본어, 중국어 간체 — 음성 없음 |
| 플랫폼 | Windows |
| 컨트롤러 | 풀 컨트롤러 지원 — 엑스박스 배열 · 스팀 덱 · 플레이스테이션 패드(스팀 입력). 오른쪽 스틱으로 조준하고, 기울이면 평타가 자동으로 나갑니다. 스틱을 놓고 RT 만 누르면 가까운 적을 겨누는 보조 조준도 있습니다(빈틈은 직접 조준한 공격만 맞힘). (`GamePad.cs` · Steamworks 「Full controller support」) |
| 플레이어 | 싱글플레이어 |
| 스팀 기능 | 스팀 도전 과제(38개) · 스팀 순위표(무한 · 일일 도전) |

---

## 한국어

**짧은 설명**

```
자동 조준이 없는 서바이버 액션. 마우스로 조준해 쏘고, 장전하고, 시위를 당긴다. 유령은 적에게 빙의하고 시계공은 3초를 되감는다 — 영웅 9명, 9개의 손. 보스의 빈틈은 직접 조준한 공격만 찌르고, 보스는 네가 가장 많이 쓴 공격을 기억해 막는다. 결계 안에서 쓰러뜨리는 순간 무기가 진화하는 도트 로그라이트.
```

**상세 설명**

```
[h2]자동 조준이 없다[/h2]
모든 탄은 마우스로 직접 겨누어 쏩니다. 탄창이 비면 장전하고, 활은 시위를 끝까지 당긴 순간에 놓아야 [b]「완벽!」[/b]이 됩니다. 패드는 오른쪽 스틱으로 조준하고, 스틱을 놓고 RT 만 누르면 가까운 적을 겨누는 보조 조준도 있습니다(빈틈은 직접 조준한 공격만 맞힙니다).

[h2]영웅 9명, 9개의 손[/h2]
킹 슬라임이 초원을 집어삼키자, 땅 밑에 잠든 망자들까지 깨어났다. 초원에서 지하 묘역, 불타는 지옥을 지나 영혼의 심연까지 내려갈 영웅을 고르세요.
[b]10칸의 캐릭터 도감[/b]에는 저마다 다른 무기와 우클릭 기술을 가진 영웅들이 기다립니다. 처음에는 셋만 함께하고, 나머지는 적을 처치해 모은 포인트와 판에서 이룬 일로 한 명씩 불러옵니다.
[list]
[*][b]거너[/b] — 리볼버와 시간을 늦추고 여러 적을 한꺼번에 노리는 일제 사격
[*][b]검사[/b] — 장검을 크게 휘둘러 적 무리를 직접 베고, 누를수록 강해지는 회전 베기
[*][b]도적[/b] — 쉴 새 없는 표창 세례와 무적 출혈 돌진
[*][b]궁수[/b] — 당길수록 강해지는 화살과 하늘을 덮는 화살비
[*][b]연금술사[/b] — 터지는 플라스크와 누를수록 커지는 대폭발
[*][b]지도 제작자[/b] — 깃펜으로 그은 선으로 적을 베고, 세 획마다 그려지는 도형으로 안쪽을 짓이김
[*][b]시계공[/b] — 맞힐수록 차는 시계 눈금으로 정각의 큰 톱니를 던지고, 위기에는 3초 전으로 되감기
[*][b]우체부[/b] — 가장 먼 적부터 찾아가는 편지와, 붙여 둔 표식을 한꺼번에 터뜨리는 속달
[*][b]유령[/b] — 차가운 손길로 적을 얼리고, 적의 몸에 빙의해 끌고 다니다가 안에서 터뜨림
[/list]
영웅마다 시작과 엔딩 이야기도 다릅니다.

[h2]보스의 빈틈 · 기억하는 보스[/h2]
[list]
[*][b]빈틈[/b] — 보스와 중간 보스의 몸에 빛나는 빈틈이 열립니다. 직접 조준한 탄 · 베기 · 투척만 빈틈을 찌르고, 유도탄이나 자동으로 겨누는 공격은 닿지 않습니다
[*][b]기억하는 보스[/b] — 보스의 체력이 정해진 값에 닿으면, 이번 싸움에서 가장 많이 맞은 공격을 기억해 방패나 결로 막습니다. 다른 공격으로 몰아치면 기억이 깨지고 보스가 휘청입니다
[/list]

[h2]보스를 쓰러뜨리면 무기가 진화한다[/h2]
스테이지 보스를 쓰러뜨릴 때마다 지금 무기가 부서지며 [b]진화 카드 3장[/b]이 펼쳐집니다. 판마다 두 번, 되돌릴 수 없는 선택입니다.
[list]
[*][b]거너[/b] — 권총이 화염 방사기 · 영혼 저격총 · 저주받은 쌍권총으로, 다시 레일건 · 번개 사슬총 · 부메랑 낫 등으로
[*][b]다른 영웅[/b] — 평타에 새 능력이 붙고(대지의 일격 · 독 표창 · 투창 화살 · 자석 플라스크 …), 진화 무기의 필살기가 우클릭 필살기에 덧붙습니다
[/list]
지금까지 고른 레벨업 카드와 영혼 트리 강화는 진화한 무기로 그대로 이어집니다.

[h2]100칸이 넘는 영혼 트리[/h2]
적을 쓰러뜨려 모은 [b]영혼 조각[/b]으로 [b]무기 · 필살기 · 운명 · 생존 · 영혼 · 재물[/b] 여섯 가지의 칸을 배웁니다.
[list]
[*]영웅마다 다른 트리 — 전용 무기 칸, 패시브와 그 진화
[*]배운 칸의 다음 칸만 드러나는 숨겨진 가지
[*][b]운명[/b] 가지 — 레벨업 카드 다시 뽑기, 시간의 틈, 중력 우물, 저주 전이, 영혼 메아리 …
[*]지금 무기에 쓸모없는 칸과 카드는 나오지 않습니다
[/list]

[h2]고유 스킬 50종 · 스킬 진화 37종[/h2]
레벨이 오를 때마다 카드 세 장 중 하나를 고릅니다. 영웅마다 [b]고유 스킬 10종[/b]이 따로 있어, 같은 영웅이라도 판마다 전혀 다른 싸움이 됩니다.
[list]
[*]정해진 [b]2~3개의 스킬을 모두 최대로 올리면 하나로 합쳐지는 스킬 진화[/b] — 재료의 능력은 그대로 남고 새로운 능력이 더해집니다
[*]레벨업 카드 위에 그 카드가 들어가는 진화 조합과 남은 단계가 보이고, 진화에 가까운 카드는 더 자주 나오며 가장 가까운 카드에는 [b]「★ 추천」[/b]이 반짝입니다
[*]최대 레벨은 50 — 모든 스킬을 올릴 수 없으니 어떤 진화를 노릴지가 곧 빌드입니다. 원하지 않는 카드는 [b]건너뛰기[/b]
[*]메인 메뉴의 [b]스킬 진화 도감[/b]에서 영웅별 조합을 미리 볼 수 있습니다
[/list]

[h2]보스의 필살기 · 결계 침식[/h2]
보스를 때릴수록 보스의 필살기 게이지가 찹니다. 가득 차면 보스가 결계의 이름을 외치고, [b]맵 전체가 어둠에 잠긴 채 거대한 결계[/b]가 펼쳐집니다. 그 안에 쏟아지는 공격을 버텨 내세요.
[list]
[*]리치 왕 「망자의 묘역」 — 회전하는 영혼 광선과 틈이 있는 저주의 고리
[*]지옥의 군주 「연옥 낙화」 — 곳곳의 운석 비, 내 자리를 노리는 거대 운석, 틈이 있는 운석 행진
[*]킹 슬라임 「산성 범람」 — 결계 벽에 튕기는 산성 덩어리와 번갈아 솟는 간헐천
[*]거울의 군주 「거울의 방」 — 둘레의 거울들이 차례로 광선을 되쏘고 깨진 거울 조각이 쏟아집니다
[/list]

[h2]판마다 달라지는 갈림길[/h2]
[list]
[*][b]장 중반 습격[/b] — 둘레에서 한꺼번에 몰려오는 적 무리를 모두 쓰러뜨리면 [b]보물 상자[/b]가 나타나고, 코인 · 영혼 조각 · 생명 중 하나를 고릅니다
[*][b]보스 보상[/b] — 장 보스를 쓰러뜨리면 전설의 힘 · 영혼 조각 다발 · 생명의 정수 중 하나를 고릅니다
[*][b]저주 제단[/b] — 2장부터 저주를 받으면 적이 단단해지는 대신 경험치 · 코인을 더 얻습니다
[*][b]최후의 보루[/b] — 체력이 크게 떨어지면 장마다 한 번 무적이 되며 둘레의 적과 탄을 밀어냅니다
[*][b]영웅 숙련[/b] — 마지막 보스를 쓰러뜨릴 때마다 그 영웅이 판을 시작할 때 레벨업 카드를 한 장 더 고릅니다
[/list]

[h2]네 개의 세계, 네 명의 왕[/h2]
초원의 킹 슬라임, 지하 묘역의 리치 왕, 불타는 지옥의 군주, 그리고 마지막 [b]영혼의 심연[/b]에서 기다리는 [b]거울의 군주[/b]. 거울의 군주는 내 평타 · 필살기 · 레벨업 스킬 · 특수 능력을 그대로 흉내 내 돌려줍니다. 보스마다 등장 연출과 대사, 체력이 줄면 시작되는 특수 패턴, 필살기 결계가 기다립니다.

[h2]난이도와 무한 모드[/h2]
쉬움 · 보통 · 어려움 세 난이도. 보통을 클리어하면 모든 괴물과 보스가 끝없이 몰려오는 [b]불타는 사막 무한 모드[/b]가 열립니다.

[h2]스킨 상점[/h2]
모은 포인트로 [b]캐릭터 스킨 20종 · 무기 스킨 10종 · 이펙트 스킨 4종[/b]을 삽니다. 등급이 오를수록 장식, 움직일 때 잔상, 오라, 처치 연출과 전용 소리가 더해집니다. 스킨은 겉모습과 소리만 바꾸고 판정은 그대로입니다.

[h2]그 밖에[/h2]
[list]
[*]모든 우클릭 스킬은 꾹 눌러 범위를 보며 조준
[*]적 · 보스 · 무기 · 운명 · 진화 · 스킬 진화 도감
[*]머리 위로 튀는 피해 숫자, 화려한 도트 이펙트와 스테이지별 음악
[*]스팀 도전 과제 38개
[*]방송 · 수익 창출 영상 자유 (허락 필요 없음, 음악도 직접 만든 곡이라 저작권 걱정 없음)
[*]한국어 · English · 日本語 · 简体中文
[/list]
```

## English

**Short description**

```
A survivors-like with no auto-aim. Aim, reload, draw the bow. Possess foes as the Ghost, rewind time as the Clockmaker: nine heroes, nine hands. Only aimed hits strike a boss's weak point, and bosses learn to block your top move. Beat one in its barrier and your weapon evolves on the spot.
```

**About this game**

```
[h2]No auto-aim[/h2]
You aim and fire every shot with the mouse. Reload when the magazine runs dry, and release the bow the instant it is fully drawn for a [b]Perfect![/b] On a controller you aim with the right stick, and an optional assist aims at the nearest enemy when you let go of the stick and hold RT (only aimed hits can strike a weak point).

[h2]Nine heroes, nine hands[/h2]
When the King Slime swallowed the Meadow, even the dead sleeping beneath the earth began to stir. Choose the hero who will descend from the Meadow through the Catacombs and the Burning Hell, all the way to the Abyss of Souls.
A [b]10-slot character codex[/b] holds heroes with their own weapons and right-click skills. Three join you from the start; the rest are unlocked one by one with points earned from defeating enemies and with feats from your runs.
[list]
[*][b]Gunner[/b] — a revolver and Volley, a time-slowing shot at every enemy you mark
[*][b]Swordsman[/b] — swing a longsword wide to cut through crowds, plus a spin slash that grows as you hold
[*][b]Rogue[/b] — a nonstop hail of throwing stars and an invincible Bleeding Dash
[*][b]Archer[/b] — arrows that grow stronger the longer you draw, and a sky-darkening arrow rain
[*][b]Alchemist[/b] — exploding flasks and a mega blast that grows as you hold
[*][b]Cartographer[/b] — cut enemies with lines drawn by a quill; every third stroke closes a shape that crushes everything inside
[*][b]Clockmaker[/b] — fill the clock with hits to throw a giant gear on the hour, and rewind three seconds when in danger
[*][b]Postman[/b] — letters that seek out the farthest foe, and an Express delivery that bursts every marked letter at once
[*][b]Ghost[/b] — a chilling touch that freezes foes, and possession: drag an enemy's body around, then burst it from inside
[/list]
Every hero also has their own opening and ending story.

[h2]Weak points and bosses that remember[/h2]
[list]
[*][b]Weak points[/b] — glowing weak points open on bosses and mini-bosses. Only shots, slashes and throws you aim yourself can strike them; homing shots and auto-targeted attacks never do
[*][b]Bosses that remember[/b] — when a boss's health reaches set marks, it remembers the attack that hit it most this fight and blocks it with a shield or a ward. Switch to other attacks to break the memory and stagger the boss
[/list]

[h2]Defeat a boss, evolve your weapon[/h2]
Each time you defeat a stage boss, your weapon shatters and [b]three evolution cards[/b] appear. Twice per run, and there's no going back.
[list]
[*][b]Gunner[/b] — the pistol becomes a flamethrower, soul sniper or cursed dual pistols, then a railgun, chain-lightning gun, boomerang scythe and more
[*][b]Other heroes[/b] — the basic attack gains a new power (Earth Smash, venom shuriken, javelin arrow, magnet flask …) and the evolved weapon's ultimate is added to your right-click ultimate
[/list]
Your level-up cards and Soul Tree upgrades carry over to the evolved weapon.

[h2]A Soul Tree of 100+ nodes[/h2]
Spend the [b]soul shards[/b] dropped by enemies on six branches: [b]Weapon, Ultimate, Fate, Survival, Soul and Wealth[/b].
[list]
[*]A different tree for every hero — unique weapon nodes, passives and their evolutions
[*]Hidden branches that reveal only the next node as you learn
[*]The [b]Fate[/b] branch — reroll level-up cards, Time Rift, Gravity Well, Curse Transfer, Soul Echo …
[*]Nodes and cards that don't fit your current weapon simply don't appear
[/list]

[h2]50 unique skills · 37 skill evolutions[/h2]
Each level-up offers three cards. Every hero has [b]10 unique skills[/b] of their own, so the same hero can fight a completely different way every run.
[list]
[*][b]Max out two or three specific skills and they fuse into a skill evolution[/b] — the ingredients keep their powers and a brand-new one is added
[*]Level-up cards show which evolution they feed into and how many ranks are left; cards close to an evolution show up more often, and the closest one sparkles as [b]"★ Recommended"[/b]
[*]Levels cap at 50 — you can't max everything, so the evolutions you chase are your build. [b]Skip[/b] the cards you don't want
[*]Browse every hero's recipes in the [b]Skill Evolution Codex[/b] on the main menu
[/list]

[h2]Boss ultimates · Barrier Erosion[/h2]
Every hit you land fills the boss's ultimate gauge. When it's full, the boss calls out its barrier's name and [b]the whole map sinks into darkness inside a huge barrier[/b]. Survive the storm of attacks within.
[list]
[*]Lich King "Graveyard of the Dead" — sweeping soul beams and cursed rings with a single gap
[*]Demon Lord "Purgatory's Fall" — meteor showers, a giant meteor aimed at you and a marching meteor line with a gap
[*]King Slime "Acid Deluge" — acid blobs bouncing off the barrier walls and alternating geysers
[*]Mirror Sovereign "Hall of Mirrors" — the mirrors around you take turns firing beams back while broken shards rain down
[/list]

[h2]A run that branches[/h2]
[list]
[*][b]Mid-chapter ambush[/b] — defeat the horde that surrounds you all at once and a [b]treasure chest[/b] appears: pick coins, soul shards or life
[*][b]Boss rewards[/b] — after each chapter boss, choose Legendary Might, a Soul Shard Bundle or the Essence of Life
[*][b]Cursed Altar[/b] — from chapter 2, accept a curse for tougher enemies and more EXP and coins
[*][b]Last Stand[/b] — once per chapter at low health, become invincible and blast away nearby enemies and bullets
[*][b]Hero Mastery[/b] — every final boss you defeat lets that hero pick one more level-up card at the start of a run
[/list]

[h2]Four worlds, four kings[/h2]
The King Slime of the Meadow, the Lich King of the Catacombs, the Demon Lord of the Burning Hell — and in the final [b]Abyss of Souls[/b], the [b]Mirror Sovereign[/b], who copies your basic attack, ultimate, level-up skills and special abilities and turns them back on you. Every boss has a cinematic entrance, lines of their own, special patterns once their health runs low and an ultimate barrier.

[h2]Difficulties and Endless Mode[/h2]
Easy, Normal and Hard. Clear Normal to unlock the [b]Burning Desert Endless Mode[/b], where every monster and boss keeps coming.

[h2]Skin Shop[/h2]
Spend your points on [b]20 character skins, 10 weapon skins and 4 effect skins[/b]. Higher tiers add ornaments, motion trails, auras, kill effects and unique sounds. Skins only change looks and sounds, never hitboxes.

[h2]And more[/h2]
[list]
[*]Hold right-click to aim every skill with a range preview
[*]Codexes for enemies, bosses, weapons, Fate, evolutions and skill evolutions
[*]Damage numbers, flashy pixel effects and music for every stage
[*]38 Steam achievements
[*]Streaming and monetized videos welcome — no permission needed, all music is original
[*]한국어 · English · 日本語 · 简体中文
[/list]
```

## 日本語

**短い説明**

```
オートエイムのないサバイバーアクション。マウスで狙って撃ち、装填し、弓を引き絞る。幽霊は敵に憑依し、時計職人は3秒を巻き戻す——9人の英雄、9つの手触り。ボスの隙を突けるのは狙った攻撃だけ。ボスはお前が一番使った攻撃を覚えて防ぐ。結界の中で倒した瞬間、武器がその場で進化するドット絵ローグライト。
```

**このゲームについて**

```
[h2]オートエイムはない[/h2]
すべての弾はマウスで自分で狙って撃つ。弾倉が空になれば装填し、弓は引き絞った瞬間に放てば[b]「パーフェクト！」[/b]になる。パッドでは右スティックで狙い、スティックを離してRTだけを押すと近くの敵を狙う補助照準もある（隙を突けるのは自分で狙った攻撃だけ）。

[h2]9人の英雄、9つの手触り[/h2]
キングスライムが草原を飲み込むと、地の底に眠る亡者までもが目覚め始めた。草原から地下墓所、燃える地獄を抜けて魂の深淵まで降りていく英雄を選ぼう。
[b]10枠のキャラクター図鑑[/b]には、それぞれ異なる武器と右クリック技を持つ英雄たちが待っている。最初に仲間になるのは三人、残りは敵を倒して集めたポイントと、ランで成し遂げたことで一人ずつ解放。
[list]
[*][b]ガンナー[/b] — リボルバーと、時間を遅くして狙った敵へ一斉に撃つ一斉射撃
[*][b]剣士[/b] — 長剣を大きく振るって敵の群れを直接斬り、押すほど強くなる回転斬り
[*][b]盗賊[/b] — 絶え間ない手裏剣の雨と無敵の出血突進
[*][b]弓使い[/b] — 引くほど強くなる矢と空を覆う矢の雨
[*][b]錬金術師[/b] — 弾けるフラスコと押すほど大きくなる大爆発
[*][b]地図職人[/b] — 羽ペンで引いた線で敵を斬り、三画ごとに描かれる図形で内側を押し潰す
[*][b]時計職人[/b] — 当てるほど溜まる時計の目盛りで正時の大歯車を投げ、危機には3秒前へ巻き戻す
[*][b]郵便配達人[/b] — 一番遠い敵から届く手紙と、付けた印をまとめて弾けさせる速達
[*][b]幽霊[/b] — 冷たい手で敵を凍らせ、敵の体に憑依して引きずり回し、内側から弾けさせる
[/list]
英雄ごとにオープニングとエンディングの物語も異なる。

[h2]ボスの隙・覚えるボス[/h2]
[list]
[*][b]隙[/b] — ボスと中ボスの体に光る隙が開く。自分で狙った弾・斬撃・投擲だけが隙を突き、追尾弾や自動で狙う攻撃は届かない
[*][b]覚えるボス[/b] — ボスの体力が決まった値に達すると、この戦いで一番多く受けた攻撃を覚えて盾や結で防ぐ。別の攻撃で押し切れば記憶が砕け、ボスがよろめく
[/list]

[h2]ボスを倒すと武器が進化する[/h2]
ステージボスを倒すたびに今の武器が砕け、[b]進化カード3枚[/b]が広がる。1回のプレイで2回、やり直しのきかない選択だ。
[list]
[*][b]ガンナー[/b] — 拳銃が火炎放射器・魂の狙撃銃・呪われた二丁拳銃に、さらにレールガン・連鎖雷銃・ブーメラン鎌などへ
[*][b]他の英雄[/b] — 通常攻撃に新たな力が加わり（大地の一撃・毒手裏剣・投げ槍の矢・磁石フラスコ …）、進化武器の必殺技が右クリック必殺技に上乗せされる
[/list]
選んだレベルアップカードと魂のツリーの強化は、進化した武器にそのまま引き継がれる。

[h2]100マスを超える魂のツリー[/h2]
敵を倒して集めた[b]魂のかけら[/b]で、[b]武器・必殺技・運命・生存・魂・財宝[/b]の六つの枝のマスを習得する。
[list]
[*]英雄ごとに異なるツリー — 専用の武器マス、パッシブとその進化
[*]習得したマスの次だけが現れる隠された枝
[*][b]運命[/b]の枝 — レベルアップカードの引き直し、時の裂け目、重力井戸、呪いの伝染、魂の残響 …
[*]今の武器に合わないマスやカードは出てこない
[/list]

[h2]固有スキル50種・スキル進化37種[/h2]
レベルが上がるたびに3枚のカードから1枚を選ぶ。英雄ごとに[b]固有スキル10種[/b]があり、同じ英雄でもプレイごとにまったく違う戦い方になる。
[list]
[*]決まった[b]2〜3個のスキルをすべて最大まで上げると一つに融合するスキル進化[/b] — 素材の能力はそのまま、新しい能力が加わる
[*]レベルアップカードの上にそのカードが入る進化の組み合わせと残り段階が表示され、進化に近いカードほど出やすく、いちばん近いカードには[b]「★ おすすめ」[/b]が輝く
[*]最大レベルは50 — すべては上げられないので、どの進化を狙うかがビルドそのもの。いらないカードは[b]スキップ[/b]
[*]メインメニューの[b]スキル進化図鑑[/b]で英雄ごとの組み合わせを確認できる
[/list]

[h2]ボスの必殺技・結界侵食[/h2]
ボスを攻撃するほどボスの必殺技ゲージがたまる。満タンになるとボスが結界の名を叫び、[b]マップ全体が闇に沈み巨大な結界[/b]が広がる。その中に降り注ぐ攻撃を耐え抜け。
[list]
[*]リッチ王「亡者の墓域」 — 回転する魂の光線と隙間のある呪いの輪
[*]地獄の君主「煉獄落火」 — 隕石の雨、足元を狙う巨大隕石、隙間のある隕石の行進
[*]キングスライム「酸の氾濫」 — 結界の壁で跳ね返る酸の塊と交互に噴き出す間欠泉
[*]鏡の君主「鏡の間」 — 周りの鏡が次々と光線を撃ち返し、割れた鏡の破片が降り注ぐ
[/list]

[h2]毎回変わる分かれ道[/h2]
[list]
[*][b]章の中盤の襲撃[/b] — 周りから一斉に押し寄せる敵の群れをすべて倒すと[b]宝箱[/b]が現れ、コイン・魂のかけら・生命から一つを選ぶ
[*][b]ボス報酬[/b] — 章のボスを倒すと、伝説の力・魂のかけらの束・生命の精髄から一つを選ぶ
[*][b]呪いの祭壇[/b] — 第2章から、呪いを受けると敵が硬くなる代わりに経験値とコインが増える
[*][b]最後の砦[/b] — 体力が大きく減ると章ごとに一度、無敵になって周りの敵と弾を吹き飛ばす
[*][b]英雄の熟練[/b] — 最後のボスを倒すたびに、その英雄はラン開始時にレベルアップカードをもう一枚選べる
[/list]

[h2]四つの世界、四人の王[/h2]
草原のキングスライム、地下墓所のリッチ王、燃える地獄の君主、そして最後の[b]魂の深淵[/b]で待つ[b]鏡の君主[/b]。鏡の君主はお前の通常攻撃・必殺技・レベルアップスキル・特殊能力をそのまま真似て返してくる。ボスごとの登場演出と台詞、体力が減ると始まる特殊パターン、必殺技の結界が待ち受ける。

[h2]難易度とエンドレスモード[/h2]
イージー・ノーマル・ハードの三つの難易度。ノーマルをクリアすると、すべての怪物とボスが押し寄せる[b]燃える砂漠のエンドレスモード[/b]が解放される。

[h2]スキンショップ[/h2]
集めたポイントで[b]キャラクタースキン20種・武器スキン10種・エフェクトスキン4種[/b]を購入。等級が上がるほど装飾、移動時の残像、オーラ、撃破演出と専用サウンドが加わる。スキンは見た目と音だけを変え、当たり判定はそのまま。

[h2]その他[/h2]
[list]
[*]すべての右クリック技は長押しで範囲を見ながら狙える
[*]敵・ボス・武器・運命・進化・スキル進化の図鑑
[*]頭上に飛び出すダメージ数値、派手なドットエフェクトとステージごとのBGM
[*]Steam実績38個
[*]配信・収益化動画は自由です（許可不要・音楽はすべてオリジナル曲）
[*]한국어・English・日本語・简体中文
[/list]
```

## 简体中文

**简短描述**

```
没有自动瞄准的幸存者动作游戏。用鼠标瞄准射击、换弹、拉弓。幽灵附身敌人，钟表匠倒回三秒——9位英雄，9种手感。只有亲手瞄准的攻击才能击中首领的破绽，首领还会记住你最常用的攻击并加以防御。在结界中击败首领的瞬间，武器当场进化。像素风肉鸽。
```

**关于这款游戏**

```
[h2]没有自动瞄准[/h2]
每一发子弹都由你用鼠标亲手瞄准射出。弹匣打空就要换弹，拉满弓弦的瞬间松手才是[b]「完美！」[/b]。手柄用右摇杆瞄准；松开摇杆只按RT时，也有瞄准最近敌人的辅助瞄准（只有亲手瞄准的攻击才能击中破绽）。

[h2]9位英雄，9种手感[/h2]
史莱姆王吞没了草原，连沉睡在地底的亡者也开始苏醒。选择一位英雄，从草原经地下墓穴、燃烧地狱，一路深入灵魂深渊。
[b]10格角色图鉴[/b]中，拥有各自武器与右键技能的英雄们正在等待。开局有三位同伴，其余英雄需用击败敌人累积的点数和对局中达成的成就逐一解锁。
[list]
[*][b]枪手[/b] — 左轮手枪与让时间变慢、一次射向所有锁定敌人的齐射
[*][b]剑士[/b] — 大幅挥舞长剑直接斩开敌群，按得越久越强的回旋斩
[*][b]盗贼[/b] — 连绵不断的飞镖与无敌的出血突进
[*][b]弓箭手[/b] — 拉得越久越强的箭矢与遮天箭雨
[*][b]炼金术士[/b] — 爆炸烧瓶与按得越久越大的大爆炸
[*][b]制图师[/b] — 用羽毛笔划出的线斩敌，每三笔自动画成的图形把内部碾碎
[*][b]钟表匠[/b] — 命中越多钟表刻度越满，整点掷出巨大齿轮，危急时倒回三秒前
[*][b]邮差[/b] — 从最远的敌人寄起的信，以及让所有印记一齐爆开的速达
[*][b]幽灵[/b] — 冰冷的手冻住敌人，附身敌人的身体拖着走，再从内部炸开
[/list]
每位英雄都有专属的开场与结局故事。

[h2]首领的破绽 · 会记忆的首领[/h2]
[list]
[*][b]破绽[/b] — 首领和中头目身上会出现发光的破绽。只有亲手瞄准的子弹、斩击和投掷才能击中，追踪弹和自动锁定的攻击打不到
[*][b]会记忆的首领[/b] — 首领体力降到特定值时，会记住这场战斗中受到最多的攻击，并用护盾或结界挡下。换用别的攻击猛攻，就能打碎记忆，让首领踉跄
[/list]

[h2]击败首领，武器进化[/h2]
每击败一位关卡首领，当前的武器就会碎裂，展开[b]3张进化卡牌[/b]。每局两次，一旦选择无法反悔。
[list]
[*][b]枪手[/b] — 手枪进化为火焰喷射器、灵魂狙击枪或诅咒双枪，再进化为轨道炮、连锁闪电枪、回旋镰刀等
[*][b]其他英雄[/b] — 普通攻击获得新的能力（大地一击、毒镖、投枪箭、磁力烧瓶……），进化武器的必杀技会附加于右键必杀技
[/list]
已选择的升级卡牌与灵魂树强化会原样继承到进化后的武器上。

[h2]超过100个节点的灵魂树[/h2]
用击败敌人收集的[b]灵魂碎片[/b]，学习[b]武器、必杀技、命运、生存、灵魂、财富[/b]六大分支的节点。
[list]
[*]每位英雄的树各不相同 — 专属武器节点、被动技能及其进化
[*]只显示已学节点下一格的隐藏分支
[*][b]命运[/b]分支 — 重抽升级卡牌、时间裂隙、重力井、诅咒转移、灵魂回响……
[*]与当前武器不匹配的节点和卡牌不会出现
[/list]

[h2]50种专属技能 · 37种技能进化[/h2]
每次升级从三张卡牌中选择一张。每位英雄都有[b]10种专属技能[/b]，即使是同一位英雄，每一局的战斗方式也截然不同。
[list]
[*]将指定的[b]两到三个技能全部升到满级，即可融合为技能进化[/b] — 素材的能力保留，并获得全新能力
[*]升级卡牌上会显示该卡牌所属的进化组合与剩余等级，越接近进化的卡牌出现得越频繁，最接近的卡牌会闪耀[b]「★ 推荐」[/b]
[*]等级上限为50 — 无法升满所有技能，追求哪种进化就是你的流派。不想要的卡牌可以[b]跳过[/b]
[*]可在主菜单的[b]技能进化图鉴[/b]中查看每位英雄的组合
[/list]

[h2]首领必杀技 · 结界侵蚀[/h2]
每次攻击首领都会积累首领的必杀槽。槽满时，首领会喊出结界之名，[b]整张地图沉入黑暗，巨大的结界随之展开[/b]。在其中撑过倾泻而下的攻击吧。
[list]
[*]巫妖王「亡者墓域」 — 旋转的灵魂光束与带缺口的诅咒之环
[*]地狱领主「炼狱落火」 — 陨石雨、瞄准脚下的巨型陨石与留有缺口的陨石行进
[*]史莱姆王「酸性泛滥」 — 在结界壁上反弹的酸液团与交替喷发的间歇泉
[*]镜之君主「镜之间」 — 周围的镜子轮流反射光束，破碎的镜片倾泻而下
[/list]

[h2]每局不同的岔路[/h2]
[list]
[*][b]章节中段袭击[/b] — 击败从四周一齐涌来的敌群后会出现[b]宝箱[/b]，可从金币、灵魂碎片、生命中选择一项
[*][b]首领奖励[/b] — 击败章节首领后，从传说之力、一束灵魂碎片、生命精华中选择一项
[*][b]诅咒祭坛[/b] — 从第2章开始，接受诅咒会让敌人更强，但获得更多经验与金币
[*][b]最后的堡垒[/b] — 生命大幅下降时，每章一次进入无敌并震开周围的敌人与子弹
[*][b]英雄熟练[/b] — 每击败一次最终首领，该英雄开局时可多选一张升级卡牌
[/list]

[h2]四个世界，四位王者[/h2]
草原的史莱姆王、地下墓穴的巫妖王、燃烧地狱的领主，以及在最后的[b]灵魂深渊[/b]等待的[b]镜之君主[/b]——它会模仿你的普通攻击、必杀技、升级技能与特殊能力并原样奉还。每位首领都有登场演出与台词，血量降低后会施展特殊攻击模式，还会展开必杀结界。

[h2]难度与无尽模式[/h2]
简单、普通、困难三种难度。通关普通后将解锁所有怪物与首领源源不断涌来的[b]燃烧沙漠无尽模式[/b]。

[h2]皮肤商店[/h2]
用累积的点数购买[b]20款角色皮肤、10款武器皮肤与4款特效皮肤[/b]。品级越高，装饰、移动残影、光环、击杀演出与专属音效就越丰富。皮肤只改变外观与声音，判定范围保持不变。

[h2]更多内容[/h2]
[list]
[*]所有右键技能均可长按，查看范围后瞄准
[*]敌人·首领·武器·命运·进化·技能进化图鉴
[*]头顶弹出的伤害数字、华丽的像素特效与各关卡专属音乐
[*]38个Steam成就
[*]欢迎直播与发布带收益的视频——无需许可，所有音乐均为原创
[*]한국어 · English · 日本語 · 简体中文
[/list]
```

---

## 시스템 요구 사항 (최소 = 권장)

| 항목 | 값 |
|---|---|
| 운영체제 | Windows 10 64비트 |
| 프로세서 | 듀얼 코어 2.0 GHz |
| 메모리 | 4 GB RAM |
| 그래픽 | DirectX 11 지원 그래픽 카드 |
| DirectX | 버전 11 |
| 저장 공간 | 300 MB |
