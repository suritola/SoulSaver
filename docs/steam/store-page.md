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
> 2026-10-18: 사용자 지시로 영웅 설명 · 캐릭터 수 등 세부 내용을 빼고 짧게 다시 씀

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
직접 조준하고 장전하는 손맛, 자동 조준이 없는 도트 서바이버 로그라이트입니다. 보스의 빈틈은 직접 조준한 공격만 찌를 수 있고, 보스는 가장 많이 쓴 공격을 기억해 막습니다. 결계 안에서 보스를 쓰러뜨리면 그 자리에서 무기가 진화합니다.
```

**상세 설명**

```
[h2]자동 조준이 없다[/h2]
모든 공격은 직접 겨누어 쏩니다. 탄창이 비면 장전하고, 활은 시위를 끝까지 당긴 순간에 놓아야 [b]「완벽!」[/b]이 됩니다. 패드는 오른쪽 스틱으로 조준합니다. 스틱을 놓고 RT 만 누르면 가까운 적을 겨누는 보조 조준도 있지만, 보조 조준으로는 빈틈을 맞힐 수 없습니다.

[h2]결계 속 보스와의 싸움[/h2]
[list]
[*][b]결계[/b] — 보스는 거대한 결계를 펼쳐 맵을 어둠에 잠그고 공격을 쏟아붓습니다.
[*][b]빈틈[/b] — 보스의 몸에 빛나는 빈틈이 열립니다. 직접 조준한 공격만 빈틈을 찌를 수 있습니다.
[*][b]기억하는 보스[/b] — 보스는 이번 싸움에서 가장 많이 맞은 공격을 기억해 막습니다. 다른 공격으로 몰아치면 기억이 깨집니다.
[/list]

[h2]쓰러뜨리면 무기가 진화한다[/h2]
보스를 쓰러뜨리면 그 자리에서 무기가 부서지며 진화합니다. 레벨업 카드와 영혼 트리로 쌓은 힘은 진화한 무기로 그대로 이어지고, 판마다 전혀 다른 빌드가 만들어집니다.

[h2]네 개의 세계와 끝없는 도전[/h2]
초원에서 영혼의 심연까지, 네 개의 세계를 내려가며 싸웁니다. 끝까지 내려간 뒤에는 무한 모드와 일일 도전이 기다립니다.

[h2]그 밖에[/h2]
[list]
[*]방송 · 수익 창출 영상 자유 (허락 필요 없음, 음악도 직접 만든 곡이라 저작권 걱정 없음)
[*]한국어 · English · 日本語 · 简体中文
[/list]
```

## English

**Short description**

```
A pixel-art survivors-like roguelite with no auto-aim: you aim and reload every shot yourself. Only aimed hits strike a boss's weak point, and bosses remember your most-used attack and block it. Defeat a boss inside its barrier and your weapon evolves on the spot.
```

**About this game**

```
[h2]No auto-aim[/h2]
You aim every attack yourself. Reload when the magazine runs dry, and release the bow the instant it is fully drawn for a [b]Perfect![/b] On a controller you aim with the right stick. Let go of the stick and hold RT for an optional assist that aims at the nearest enemy, but assisted shots can never strike a weak point.

[h2]Boss fights inside the barrier[/h2]
[list]
[*][b]Barriers[/b] — bosses raise a huge barrier, plunge the map into darkness and unleash a storm of attacks.
[*][b]Weak points[/b] — glowing weak points open on a boss's body. Only attacks you aim yourself can strike them.
[*][b]Bosses that remember[/b] — a boss remembers the attack that hit it most this fight and blocks it. Switch to other attacks to break the memory.
[/list]

[h2]Defeat a boss, evolve your weapon[/h2]
Defeat a boss and your weapon shatters and evolves on the spot. The power you built with level-up cards and the Soul Tree carries over to the evolved weapon, so every run grows into a completely different build.

[h2]Four worlds and endless challenges[/h2]
Fight your way down through four worlds, from the Meadow to the Abyss of Souls. Once you reach the bottom, Endless Mode and Daily Challenges await.

[h2]And more[/h2]
[list]
[*]Streaming and monetized videos welcome — no permission needed, all music is original
[*]한국어 · English · 日本語 · 简体中文
[/list]
```

## 日本語

**短い説明**

```
自分で狙い、装填する手応えを味わう、オートエイムのないドット絵サバイバー・ローグライトです。ボスの隙は自分で狙った攻撃でしか突けず、ボスは一番多く使った攻撃を覚えて防ぎます。結界の中でボスを倒すと、その場で武器が進化します。
```

**このゲームについて**

```
[h2]オートエイムはない[/h2]
すべての攻撃は自分で狙って放ちます。弾倉が空になれば装填し、弓は引き絞った瞬間に放てば[b]「パーフェクト！」[/b]になります。パッドでは右スティックで狙います。スティックを離してRTだけを押すと近くの敵を狙う補助照準もありますが、補助照準では隙を突けません。

[h2]結界の中のボス戦[/h2]
[list]
[*][b]結界[/b] — ボスは巨大な結界を広げてマップを闇に沈め、攻撃を降り注がせます。
[*][b]隙[/b] — ボスの体に光る隙が開きます。隙を突けるのは自分で狙った攻撃だけです。
[*][b]覚えるボス[/b] — ボスはこの戦いで一番多く受けた攻撃を覚えて防ぎます。別の攻撃で押し切れば記憶が砕けます。
[/list]

[h2]倒せば武器が進化する[/h2]
ボスを倒すと、その場で武器が砕けて進化します。レベルアップカードと魂のツリーで積み上げた力は進化した武器にそのまま引き継がれ、毎回まったく違うビルドが生まれます。

[h2]四つの世界と終わりなき挑戦[/h2]
草原から魂の深淵まで、四つの世界を降りながら戦います。最後までたどり着いた後には、エンドレスモードとデイリーチャレンジが待っています。

[h2]その他[/h2]
[list]
[*]配信・収益化動画は自由です（許可不要・音楽はすべてオリジナル曲）
[*]한국어・English・日本語・简体中文
[/list]
```

## 简体中文

**简短描述**

```
亲手瞄准、亲手换弹的手感——没有自动瞄准的像素风幸存者肉鸽游戏。只有亲手瞄准的攻击才能击中首领的破绽，首领还会记住你最常用的攻击并加以防御。在结界中击败首领，武器当场进化。
```

**关于这款游戏**

```
[h2]没有自动瞄准[/h2]
每一次攻击都由你亲手瞄准。弹匣打空就要换弹，拉满弓弦的瞬间松手才是[b]「完美！」[/b]。手柄用右摇杆瞄准。松开摇杆只按RT时，也有瞄准最近敌人的辅助瞄准，但辅助瞄准无法击中破绽。

[h2]结界中的首领战[/h2]
[list]
[*][b]结界[/b] — 首领会展开巨大的结界，让地图沉入黑暗，倾泻攻击。
[*][b]破绽[/b] — 首领身上会出现发光的破绽。只有亲手瞄准的攻击才能击中。
[*][b]会记忆的首领[/b] — 首领会记住这场战斗中受到最多的攻击并加以防御。换用别的攻击猛攻，就能打碎记忆。
[/list]

[h2]击败首领，武器进化[/h2]
击败首领后，武器会当场碎裂并进化。用升级卡牌与灵魂树积累的力量会原样继承到进化后的武器上，每一局都会成长为截然不同的流派。

[h2]四个世界与无尽挑战[/h2]
从草原一路深入灵魂深渊，在四个世界中层层战斗。抵达终点后，无尽模式与每日挑战正等待着你。

[h2]更多内容[/h2]
[list]
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
