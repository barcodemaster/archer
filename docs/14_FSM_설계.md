# 13. Pet System (펫 시스템)

## 개요
플레이어를 따라다니며 자동으로 전투하는 동반자 시스템. 장비 슬롯(Pet1/Pet2)에 장착하면 펫이 스폰되며, 플레이어 주변에 offset으로 고정되어 가장 가까운 몬스터를 자동 공격한다.

## 설계 원칙
- 플레이어에 **offset 고정** (자유 이동 X, 물리 시뮬레이션 불필요)
- 플레이어와 **위치 + 회전 동기화**
- **사거리 제한 없음** (항상 가장 가까운 몬스터 공격)
- State Machine 불필요 — **단순 Update 기반** 공격 로직
- 기존 장비 시스템과 완전 통합 (장착/해제로 스폰/디스폰)

## 아키텍처

### 컴포넌트 구조
```
PlayerController (GameObject)
  └── PetSpawner.cs         ← EquipmentManager.OnEquipChanged 구독
        ├── PetController(Pet1)  ← 동적 스폰
        └── PetController(Pet2)  ← 동적 스폰
```

### PetController.cs (PetBase.cs 교체)
- `Init(data, table, slot)`: 장비 데이터로 초기화, offset/스탯 계산
- `FollowPlayer()`: 매 프레임 플레이어 위치 + 회전 기준 offset 적용
- `UpdateTarget()`: 캐시 주기마다 최근접 몬스터 갱신 (`StageManager.AliveMonsters`)
- `AutoAttack()`: 쿨다운 기반 자동 공격
  - **Melee**: 직접 `TakeDamage()` 호출
  - **Ranged**: `ProjectileFactory.Create()` 사용
  - **Buffer**: `PlayerController.Heal()` 호출

### PetSpawner.cs
- `EquipmentManager.OnEquipChanged` 이벤트 구독
- Pet1/Pet2 슬롯 상태 변경 시 자동 스폰/디스폰
- 같은 장비의 레벨업 시 `RefreshStats()`만 호출 (재스폰 없음)
- 다른 장비로 교체 시 디스폰 후 재스폰

### 발사체 설정
| Inspector 필드 | 설명 |
|----------------|------|
| `_useArc` = true | Arc(포물선) 발사체 — `ProjectileFactory.CreateArc()` |
| `_useArc` = false | Straight(직선) 발사체 — `ProjectileFactory.Create()` |

## 밸런스 (GameConfig)
| 설정 | 기본값 | 설명 |
|------|--------|------|
| petOffset1 | (-0.8, 0, -0.5) | Pet1 로컬 좌표 오프셋 |
| petOffset2 | (0.8, 0, -0.5) | Pet2 로컬 좌표 오프셋 |
| petAttackCooldown | 1.5초 | 공격 쿨다운 |
| petTargetCacheInterval | 0.2초 | 타겟 캐싱 주기 |
| petScale | 0.5 | 펫 스케일 |

## 기존 시스템 재사용
- `EquipmentManager.OnEquipChanged`: 스폰 트리거
- `EquipmentData.GetMainStat()`: 데미지 계산
- `ProjectileFactory.Create()`: Ranged 펫 투사체
- `StageManager.AliveMonsters`: 타겟 탐색
- `GameConfig.Instance`: 밸런스 값 중앙 관리

## 디자인 패턴
- **Observer Pattern**: `OnEquipChanged` 이벤트로 스폰 트리거
- **Factory Pattern**: `ProjectileFactory`로 원거리 펫 투사체 생성
- **Component Pattern**: Player에 `PetSpawner` 부착, 동적 `PetController` 생성
