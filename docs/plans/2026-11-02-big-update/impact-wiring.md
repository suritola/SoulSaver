# 손맛 연출(Impact) 연결 지점 — 2.0.0 대규모 업데이트

엔진: `Assets/Scripts/Impact.cs` (규칙 · 상한 · 쉬는 시간 · 설정 「특수효과」) · 영웅별 서명: `Assets/Scripts/HeroImpacts.cs`
호출은 한 줄: `Impact.Hit(ImpactTier.<단계>, 월드 위치, 방향)` — 방향은 「플레이어 → 맞은 적」 정규화 벡터 (모르면 생략).
쉬는 시간 · 상한 · 설정 · 레벨업/일시정지 충돌은 엔진이 처리하므로 **호출하는 쪽은 조건을 따로 걸지 않는다**.
기존 `Shake` / 소리 / 이펙트는 **지우지 않는다** (특수효과를 끈 사람에게는 예전 그대로여야 함. 흔들림은 센 쪽이 이기게 바뀌어 겹쳐도 됨).

## 1. 평타 특수 조건 (ImpactTier.Attack) — 「그 조건이 성립해 적에게 명중 · 처치한 순간」
| 영웅 | 조건 | 위치 (조사 기준 줄 — 실제 줄은 확인) |
|---|---|---|
| 거너 | 저주받은 마지막 탄(`cursed` · `lastShell`)이 적에게 명중 | 발사 PlayerController.cs:685-698, 명중은 총알 쪽에서 찾을 것 |
| 검사 | 연속 베기 마지막 타(4번째, 2레벨부터 3번째)가 적에게 명중 | CharacterKit.cs:358-364 |
| 도적 | 평타로 **출혈 중인 적을 처치** (급소) | CharacterKit.cs:1159-1161 `TargetDamageMul` 근처 · 처치 판정 |
| 궁수 | 「완벽!」 화살이 적에게 명중 | CharacterKit.cs:505-530 (`b.perfect`) |
| 연금술사 | 불안정 플라스크가 터짐 | CharacterKit.cs:581 · CharacterKit.Alchemist.cs:98 |
| 지도 제작자 | 도형이 닫혀 안쪽 적 4마리 이상을 침 (이미 Shake 하는 곳) | CharacterKit.Cartographer.cs:225-227 |
| 시계공 | 「정각」 톱니가 적에게 명중 | CharacterKit.Clockmaker.cs:36 · :67 |
| 우체부 | 10번째 배달 우편 보너스 | CharacterKit.Postman.cs:207 · :276 |
| 유령 | 냉기 다섯 겹으로 적이 얼어붙음 | CharacterKit.Ghost.cs:171-172 |

## 2. 고유 스킬 (ImpactTier.Skill) — 한 곳에서
`SignatureSkills.Hit(target, dmg, killed)` (SignatureSkills.cs:168 부근)에서 `DamageSource.Current == DamageSource.Signature` 이고 `killed` 인 처치를 0.25초 창으로 셈 → 창 안에서 **3마리째** 처치 때 `Impact.Hit(ImpactTier.Skill, 그 적 위치)` 한 번 (창은 실제 시간 · 한 창에 한 번).

## 3. 진화 스킬 (ImpactTier.Evolve) — 한 곳에서
`SignatureSkills.Flair(key, p, size, c)` (SignatureSkills.cs:131) — 진화한 키라서 장식을 실제로 그리는 갈래 안에서 `Impact.Hit(ImpactTier.Evolve, p)`.

## 4. 필살기 (ImpactTier.Ult) — 「터지는 / 맞는 순간」 (발동 버튼 순간이 아님 · `UltUsed` 이벤트는 쓰지 않음)
| 영웅 | 위치 |
|---|---|
| 거너 | 조준 끝 사격 `EndSkill` → `ShootTargets` / `ShootBossTargets` 첫 발 (PlayerController.cs:1024 · :1116 · :1203), 즉발형 `InstantUlt` :1102 |
| 검사 | 회전 베기 발동 CharacterKit.cs:896-910 |
| 연금술사 | 큰 플라스크 착탄 콜백 CharacterKit.cs:916 |
| 도적 | `BleedDash` 명중 (흔들림 있는 곳 :995) |
| 궁수 | `ArrowRain` 화살비가 떨어지는 순간 :1072, 갈래 `BowPathRainCast` CharacterKit.Archer.cs:243 |
| 지도 제작자 | `FoldRoutine` 접히는 순간 (CharacterKit.Cartographer.cs:526) — 방향 = 접힌 방향 |
| 시계공 | `RewindRoutine` 되감기 도착 (CharacterKit.Clockmaker.cs:405) |
| 우체부 | `ExpressRoutine` 명중 (CharacterKit.Postman.cs:698) — 방향 = 날아간 방향 |
| 유령 | `PossessRoutine` 터짐 (CharacterKit.Ghost.cs:715) |

한 번의 필살기에서 여러 번 불려도 된다 (엔진이 같은 단계 연출을 상한 · 「센 쪽이 먼저」로 거름 — 단, 루프 안이면 첫 명중 한 번만 부를 것).

## 지키기
- AGENT.md 규칙 2 (게임 스크립트에 UnityEditor 금지). 주석 · 문구는 한국어, 기존 주석 말투(「2.0.0~ …」)를 따름.
- 자동 조작 · 배치 · 튜토리얼 조건은 엔진이 처리 — 호출처에서 걸지 않음.
