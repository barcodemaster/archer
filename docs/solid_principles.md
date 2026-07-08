# SOLID 원칙 구현 문서

## 포트폴리오 목적

이 문서는 Unity Archer 프로젝트에서 SOLID 원칙을 어떻게 설계하고 적용했는지를 정리한다.
단순한 이론 설명이 아니라, 실제 코드에서 어떤 문제를 해결하기 위해 각 원칙을 적용했는지 Before/After 관점으로 서술한다.

---

## 목차

1. [개요](#1-개요)
2. [SRP - 단일 책임 원칙](#2-srp---단일-책임-원칙)
3. [OCP - 개방-폐쇄 원칙](#3-ocp---개방-폐쇄-원칙)
4. [LSP - 리스코프 치환 원칙](#4-lsp---리스코프-치환-원칙)
5. [ISP - 인터페이스 분리 원칙](#5-isp---인터페이스-분리-원칙)
6. [DIP - 의존성 역전 원칙](#6-dip---의존성-역전-원칙)
7. [종합 아키텍처 다이어그램](#7-종합-아키텍처-다이어그램)
8. [전후 비교 (God Object vs SOLID)](#8-전후-비교-god-object-vs-solid)
9. [추가 디자인 패턴](#9-추가-디자인-패턴)

---

## 1. 개요

**SOLID**는 Robert C. Martin이 제안한 객체지향 설계의 5가지 원칙이다.

| 원칙 | 이름 | 핵심 개념 |
|------|------|----------|
| S | Single Responsibility Principle | 클래스는 단 하나의 변경 이유만 가져야 한다 |
| O | Open/Closed Principle | 확장에는 열려 있고, 수정에는 닫혀 있어야 한다 |
| L | Liskov Substitution Principle | 하위 타입은 상위 타입을 완전히 대체할 수 있어야 한다 |
| I | Interface Segregation Principle | 클라이언트는 사용하지 않는 메서드에 의존하면 안 된다 |
| D | Dependency Inversion Principle | 구체 클래스가 아닌 추상(인터페이스)에 의존해야 한다 |

### 이 프로젝트에서 SOLID가 필요했던 이유

초기 프로토타입은 God Object 구조였다. `StageManager` 하나가 보상 계산, 난이도 조절, 몬스터 스폰, 스테이지 진행을 모두 담당했고, `PlayerController`는 이동/전투/업그레이드/데미지 처리를 한 클래스에서 처리했다. 기능이 추가될수록 다음 문제가 생겼다:

- **수정 공포**: 보상 비율 하나를 바꾸면 스폰 로직에 영향을 줄까 두려워짐
- **테스트 불가**: 기능이 엮여 있어 단위 테스트 작성 자체가 어려움
- **확장 비용 증가**: 업그레이드 효과를 하나 추가하면 거대한 switch 문에 case를 추가해야 함

이를 해결하기 위해 SOLID 원칙을 도입하여 각 역할을 명확하게 분리했다.

---

## 2. SRP - 단일 책임 원칙

> "클래스는 하나의 이유로만 변경되어야 한다."

### 2-1. Manager 계층 분리

**Before: God Object StageManager**

```csharp
// StageManager.cs (리팩토링 전 - 단일 파일에 모든 책임)
public class StageManager : Singleton<StageManager>
{
    // 스테이지 진행 로직
    void GoToNextStage() { ... }

    // 보상 계산 로직 (책임 1 초과)
    void DropMonsterRewards(MonsterBase monster) { ... }
    int CalcGoldDrop(MonsterBase monster) { ... }

    // 난이도 조절 로직 (책임 2 초과)
    float GetDifficultyMultiplier(int stageIndex) { ... }
    void ApplyDifficultyToMonster(MonsterBase m, int stage) { ... }
}
```

**After: 책임별 3개 클래스 분리**

```csharp
// StageManager.cs - 스테이지 진행만 담당
public class StageManager : Singleton<StageManager>
{
    void GoToNextStage() { ... }
    void StartStage(int index) { ... }
    void BuildStagePlayOrder() { ... }
    void OpenExitDoor() { ... }
}

// RewardManager.cs - 보상 드롭/수집만 담당
public class RewardManager : Singleton<RewardManager>
{
    void DropMonsterRewards(MonsterBase monster, PlayerController player) { ... }
    void CollectAll() { ... }
    void CleanupAll() { ... }
    void GrantStageGold() { ... }
}

// DifficultyManager.cs - 난이도 계산만 담당
public class DifficultyManager : Singleton<DifficultyManager>
{
    float GetDifficultyMultiplier(int stageIndex) { ... }
    void ApplyToMonster(MonsterBase monster, int stageIndex) { ... }
}
```

**결과**: `RewardManager`의 보상 비율을 바꿔도 스테이지 진행 로직은 건드리지 않아도 된다. 변경 이유가 분리되었다.

---

### 2-2. 타일맵 시스템 분리

맵 데이터와 엔티티의 통과 능력은 서로 다른 관심사다.

```csharp
// TileMap.cs - 맵 데이터와 충돌 판정만 담당
public class TileMap : MonoBehaviour
{
    [SerializeField] MapData _data;  // 타일 종류 배열

    public bool CanPass(Vector2Int cell, ETilePassFlag flag) { ... }
    public ETileType GetTileType(Vector2Int cell) { ... }
    void OnDrawGizmos() { ... }  // 에디터 시각화
}

// TilePassability.cs - 엔티티별 통과 능력만 담당
public class TilePassability : MonoBehaviour
{
    public ETilePassFlag PassFlags;  // Walk / Fly / WaterWalk / WallPass

    public bool CanPassTile(ETileType tileType) { ... }
}
```

타일맵 구조를 바꾸려면 `TileMap`만, 특정 몬스터에게 비행 능력을 주려면 `TilePassability`만 수정하면 된다.

---

### 2-3. 투사체 시스템 분리

생성 책임과 동작 책임을 분리했다.

```csharp
// ProjectileFactory.cs - 생성만 담당
public static class ProjectileFactory
{
    public static ProjectileBase Create(GameObject prefab, Vector3 pos, Vector3 dir, ProjectileInitData data) { ... }
    public static ProjectileBase Create(GameObject prefab, Vector3 pos, Vector3 dir, float damage) { ... }
    public static ProjectileBase CreateArc(GameObject prefab, Vector3 pos, Vector3 targetPos, float damage) { ... }
}

// ProjectileBase.cs - 이동/충돌/데미지 처리만 담당
public class ProjectileBase : MonoBehaviour, IPoolable
{
    void UpdateStraight() { ... }
    void UpdateArc() { ... }
    void OnTriggerEnter(Collider other) { ... }
}
```

새 발사체 유형을 추가해도 팩토리 메서드 하나만 추가하면 되고, 이동 로직은 `ProjectileBase`에서만 수정한다.

---

### 2-4. GameConfig ScriptableObject로 매직 넘버 분리

```csharp
// GameConfig.cs - 모든 밸런스 수치를 하나의 ScriptableObject에 집중
// 파일: Assets/Scripts/Data/GameConfig.cs
[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    [Header("Difficulty")]
    public float difficultyPerStage = 0.06f;

    [Header("Drop Rates")]
    public float hpHeartDropChance = 0.12f;
    public float equipmentDropChance = 0.05f;

    [Header("Pet AI")]
    public float petDetectionRange = 5f;
    public float petAttackRange = 2.5f;
    public float petChaseTimeout = 4f;
    // ...
}
```

밸런스 수치를 바꿀 때 코드를 재컴파일하지 않아도 된다. 디자이너도 Unity 인스펙터에서 직접 수정할 수 있다.

---

## 3. OCP - 개방-폐쇄 원칙

> "새 기능 추가는 기존 코드 수정 없이 확장으로 이루어져야 한다."

### 3-1. State Pattern으로 몬스터 AI 확장

**Before: 분기 지옥**

```csharp
// Monster.cs (리팩토링 전)
void Update()
{
    if (_state == EState.Idle)
    {
        // idle 로직...
    }
    else if (_state == EState.Move)
    {
        // chase 로직...
    }
    else if (_state == EState.Attack)
    {
        // attack 로직...
    }
    // 새 상태 추가 = 이 함수 수정 필요
}
```

**After: State Pattern - 기존 클래스 수정 없이 상태 추가 가능**

```csharp
// IMonsterState.cs - 확장 계약
public interface IMonsterState
{
    void Enter(MonsterBase monster);
    void Update(MonsterBase monster);
    void Exit(MonsterBase monster);
}

// MonsterBase.cs - 상태 전환만 담당 (Open for extension, Closed for modification)
public class MonsterBase : MonoBehaviour, IDamageable
{
    IMonsterState _currentState;

    public void TransitionTo(IMonsterState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }

    void Update()
    {
        _currentState?.Update(this);  // 구체 상태를 모른다
    }
}

// 각 상태는 독립 클래스 - MonsterBase를 건드리지 않고 새 상태 추가 가능
public class MonsterIdleState : IMonsterState
{
    public static readonly MonsterIdleState Instance = new();
    public void Enter(MonsterBase monster) { monster.State = EState.Idle; monster.StopMovementPublic(); }
    public void Update(MonsterBase monster) { if (monster.HasTarget) monster.TransitionTo(MonsterChaseState.Instance); }
    public void Exit(MonsterBase monster) { }
}

public class MonsterChaseState : IMonsterState { ... }
public class MonsterAttackState : IMonsterState { ... }
public class MonsterDieState : IMonsterState { ... }
```

새 상태 `MonsterSkillState`를 추가하려면 새 클래스 파일 하나만 만들면 된다. `MonsterBase`는 수정하지 않는다.

---

### 3-2. State Pattern으로 펫 AI 확장

동일한 패턴을 펫 시스템에도 적용했다.

```csharp
// IPetState.cs
public interface IPetState
{
    void Enter(PetController pet);
    void Update(PetController pet);
    void Exit(PetController pet);
}

// 4개 상태 - 각각 독립 클래스 파일
// Assets/Scripts/Controllers/Pet/States/
// PetPatrolState.cs  - 플레이어 주변 순찰
// PetReturnState.cs  - 플레이어에게 복귀
// PetChaseState.cs   - 적 추격 (A* 경로탐색)
// PetAttackState.cs  - 공격 (Arc/Straight 분기)
```

펫에 새 행동 패턴(예: `PetHealState`)을 추가해도 기존 4개 상태 파일은 수정하지 않는다.

---

### 3-3. Command Pattern으로 업그레이드 효과 확장

**Before: switch 문 지옥**

```csharp
// (리팩토링 전)
void ApplyUpgrade(EUpgradeType type)
{
    switch (type)
    {
        case EUpgradeType.WallPass:    ApplyWallPass(); break;
        case EUpgradeType.WaterWalker: ApplyWaterWalk(); break;
        case EUpgradeType.Dwarf:       ApplyDwarf(); break;
        case EUpgradeType.Giant:       ApplyGiant(); break;
        // 업그레이드 추가 = 이 함수 수정 필요
    }
}
```

**After: Command Pattern + Registry - switch 문 완전 제거**

```csharp
// IUpgradeEffect.cs - Command 인터페이스
public interface IUpgradeEffect
{
    EUpgradeType Type { get; }
    void Apply(PlayerController player, PlayerUpgrade upgrade);
}

// UpgradeEffectRegistry.cs - 등록/디스패치 담당
public static class UpgradeEffectRegistry
{
    static Dictionary<EUpgradeType, IUpgradeEffect> _effects;

    static void Init()
    {
        Register(new WallPassEffect());
        Register(new WaterWalkerEffect());
        Register(new DwarfEffect());
        Register(new GiantEffect());
        Register(new HpBoostEffect());
        Register(new FastGrowthEffect());
    }

    public static void Apply(EUpgradeType type, PlayerController player, PlayerUpgrade upgrade)
    {
        if (_effects.TryGetValue(type, out var effect))
            effect.Apply(player, upgrade);
    }
}

// 각 효과 - 독립 클래스
public class WallPassEffect : IUpgradeEffect
{
    public EUpgradeType Type => EUpgradeType.WallPass;
    public void Apply(PlayerController player, PlayerUpgrade upgrade)
    {
        player.GetComponent<TilePassability>().PassFlags |= ETilePassFlag.WallPass;
        player.ApplyWallPassCollisions();
    }
}

// 새 업그레이드 추가 = 새 클래스 파일 + Registry.Register() 한 줄
// 기존 효과 클래스와 PlayerController는 수정 없음
```

---

## 4. LSP - 리스코프 치환 원칙

> "상위 타입 객체를 하위 타입 객체로 교체해도 프로그램 동작이 변하면 안 된다."

### IDamageable 인터페이스

발사체는 맞은 대상이 플레이어인지 몬스터인지 알 필요가 없다. `IDamageable`로 추상화했다.

```csharp
// Utils/Interfaces.cs
public interface IDamageable
{
    float MaxHp { get; }
    float CurrentHp { get; }
    void TakeDamage(float damage);
    void Heal(float amount);
    bool IsDead { get; }
    Transform transform { get; }
}

// PlayerController.cs - IDamageable 구현
public class PlayerController : MonoBehaviour, IDamageable
{
    public float MaxHp => _maxHp + EquipmentManager.Instance.GetTotalMaxHpBonus();
    public float CurrentHp => _currentHp;
    public bool IsDead => _currentHp <= 0;
    public void TakeDamage(float damage) { ... }
    public void Heal(float amount) { ... }
}

// MonsterBase.cs - IDamageable 구현
public class MonsterBase : MonoBehaviour, IDamageable
{
    public float MaxHp => _maxHp;
    public float CurrentHp => _currentHp;
    public bool IsDead => _currentHp <= 0;
    public void TakeDamage(float damage) { ... }
    public void Heal(float amount) { ... }
}
```

**발사체 충돌 처리 - 타입 구분 없이 동일한 인터페이스 사용:**

```csharp
// ProjectileBase.cs
void OnTriggerEnter(Collider other)
{
    // 플레이어 발사체가 몬스터를 맞췄을 때
    if (other.TryGetComponent<IDamageable>(out var target))
    {
        float damage = CalculateDamage();
        target.TakeDamage(damage);  // PlayerController든 MonsterBase든 동일한 호출

        if (target.IsDead)
            OnTargetDead(target);
    }
}
```

`PlayerController`와 `MonsterBase`는 `IDamageable` 자리를 완전히 대체할 수 있다. 발사체는 구체 타입을 몰라도 된다.

---

## 5. ISP - 인터페이스 분리 원칙

> "클라이언트는 사용하지 않는 메서드에 의존하면 안 된다."

각 인터페이스는 최소한의 메서드만 포함한다. 하나의 거대한 인터페이스 대신 목적별로 분리했다.

### 인터페이스 목록

```csharp
// IDamageable - HP 관련 계약만 포함
public interface IDamageable
{
    float MaxHp { get; }
    float CurrentHp { get; }
    void TakeDamage(float damage);
    void Heal(float amount);
    bool IsDead { get; }
    Transform transform { get; }
}

// IPoolable - 오브젝트 풀 생명주기 계약만 포함
public interface IPoolable
{
    void OnPoolGet();
    void OnPoolReturn();
}

// IMonsterState - 몬스터 상태 계약만 포함
public interface IMonsterState
{
    void Enter(MonsterBase monster);
    void Update(MonsterBase monster);
    void Exit(MonsterBase monster);
}

// IPetState - 펫 상태 계약만 포함
public interface IPetState
{
    void Enter(PetController pet);
    void Update(PetController pet);
    void Exit(PetController pet);
}

// IUpgradeEffect - 업그레이드 효과 계약만 포함
public interface IUpgradeEffect
{
    EUpgradeType Type { get; }
    void Apply(PlayerController player, PlayerUpgrade upgrade);
}
```

**잘못된 예시 (ISP 위반):**

```csharp
// 만약 이렇게 했다면 - ISP 위반
public interface IGameEntity
{
    // 데미지 관련
    void TakeDamage(float damage);
    void Heal(float amount);

    // 풀 관련 - 모든 구현체에 강제됨
    void OnPoolGet();
    void OnPoolReturn();

    // 상태 관련 - 몬스터에만 필요하지만 플레이어도 구현해야 함
    void Enter(MonsterBase monster);
    void Update(MonsterBase monster);
}
// PlayerController가 OnPoolGet/Enter 등 불필요한 메서드를 구현해야 하는 문제 발생
```

인터페이스를 분리함으로써 `ProjectileBase`는 `IPoolable`만, 발사체 충돌 처리는 `IDamageable`만 알면 된다.

---

## 6. DIP - 의존성 역전 원칙

> "고수준 모듈은 저수준 모듈에 의존해서는 안 된다. 둘 다 추상에 의존해야 한다."

### 6-1. MonsterBase - 구체 상태 클래스에 의존하지 않는다

```csharp
public class MonsterBase : MonoBehaviour, IDamageable
{
    // 구체 클래스(MonsterIdleState)가 아닌 인터페이스(IMonsterState)에 의존
    IMonsterState _currentState;

    public void TransitionTo(IMonsterState newState)  // 추상에 의존
    {
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }
}

// 나쁜 예 - DIP 위반
// MonsterIdleState _idleState;   // 구체 클래스에 직접 의존
// MonsterChaseState _chaseState;
```

`MonsterBase`는 `IMonsterState`만 알고, 실제로 어떤 상태 클래스가 주입되는지 모른다.

---

### 6-2. PetController - IPetState에 의존

```csharp
public class PetController : MonoBehaviour
{
    IPetState _currentState;  // 추상에 의존

    public void TransitionTo(IPetState newState)
    {
        _currentState?.Exit(this);
        _path?.Clear();
        _pathIndex = 0;
        _currentState = newState;
        _currentState.Enter(this);
    }
}
```

---

### 6-3. UpgradeEffectRegistry - IUpgradeEffect에 의존

```csharp
public static class UpgradeEffectRegistry
{
    // Dictionary의 Value 타입이 IUpgradeEffect (추상)
    static Dictionary<EUpgradeType, IUpgradeEffect> _effects;

    // 구체 구현체(WallPassEffect 등)는 Registry만 알고 있음
    // 클라이언트(PlayerController)는 IUpgradeEffect 자체를 몰라도 됨
    public static void Apply(EUpgradeType type, PlayerController player, PlayerUpgrade upgrade)
    {
        if (_effects.TryGetValue(type, out var effect))
            effect.Apply(player, upgrade);  // 인터페이스 호출
    }
}

// PlayerController.cs - Registry를 통해 간접 호출
public void ApplyUpgradeEffect(EUpgradeType type)
{
    UpgradeEffectRegistry.Apply(type, this, _upgrade);  // 구체 효과 클래스를 모름
}
```

---

### 6-4. ProjectileFactory - ProjectileBase 추상 메서드에만 의존

```csharp
public static class ProjectileFactory
{
    public static ProjectileBase Create(GameObject prefab, Vector3 pos, Vector3 dir, ProjectileInitData data)
    {
        var go = ObjectPool.Instance.Get(prefab);
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<ProjectileBase>();
        proj.Init(data);  // ProjectileBase의 추상 메서드에만 의존
        return proj;
    }
}
```

팩토리는 `ProjectileBase` 인터페이스(Init 메서드)만 알고, 구체 발사체 서브클래스의 내부 구현을 모른다.

---

## 7. 종합 아키텍처 다이어그램

```
[SOLID 원칙별 관계도]

=== SRP: 책임 분리 ===

StageManager ──┬── 스테이지 진행 (자체 담당)
               ├──> RewardManager    (보상 드롭/수집)
               └──> DifficultyManager (난이도 계산)

TileMap ────── 맵 데이터/충돌 판정
TilePassability ─ 엔티티 통과 능력

ProjectileFactory ─ 생성 담당
ProjectileBase ─── 동작/충돌 담당

=== OCP: 확장 구조 ===

                  <<interface>>
                  IMonsterState
                      |
          ┌───────────┼───────────┐
   MonsterIdleState  MonsterChaseState  MonsterAttackState  MonsterDieState
   (확장 가능, MonsterBase 수정 불필요)

                  <<interface>>
                  IPetState
                      |
          ┌───────────┼───────────┐
   PetPatrolState  PetReturnState  PetChaseState  PetAttackState

                  <<interface>>
                  IUpgradeEffect
                      |
   ┌────────────────────────────────────┐
WallPassEffect  WaterWalkerEffect  DwarfEffect  GiantEffect  HpBoostEffect  FastGrowthEffect

=== LSP: 치환 가능성 ===

<<interface>>
IDamageable
    |
    ├── PlayerController  (플레이어 HP 관리)
    └── MonsterBase       (몬스터 HP 관리)

ProjectileBase.OnTriggerEnter(IDamageable target)
  → PlayerController 또는 MonsterBase 어느 쪽이든 동일하게 처리

=== ISP: 인터페이스 분리 ===

IDamageable ──────── PlayerController, MonsterBase
IPoolable ────────── ProjectileBase, HitEffect
IMonsterState ─────── 4개 Monster 상태 클래스
IPetState ──────────  4개 Pet 상태 클래스
IUpgradeEffect ──────  6개 업그레이드 효과 클래스

=== DIP: 추상 의존 ===

MonsterBase ──의존──> IMonsterState (구체 클래스 X)
PetController ─의존──> IPetState    (구체 클래스 X)
PlayerController ─의존──> UpgradeEffectRegistry ──의존──> IUpgradeEffect
ProjectileBase ──의존──> IDamageable (PlayerController/MonsterBase 구분 X)

=== 전체 데이터 흐름 ===

EquipmentManager.OnEquipChanged
    └──> PetSpawner (Observer)
         └──> PetController.Init()
              └──> IPetState (FSM 시작)
                   └──> PetController.FireProjectile()
                        └──> ProjectileFactory.CreateArc()
                             └──> ProjectileBase.Init()
                                  └──> OnTriggerEnter(IDamageable)
                                       └──> MonsterBase.TakeDamage()
                                            └──> AchievementManager.AddProgress(KillCount)
                                                 └──> SaveManager.Save()
```

---

## 8. 전후 비교 (God Object vs SOLID)

### Before: God Object 구조

```
StageManager (1개 파일, ~600줄)
├── 스테이지 진행
├── 보상 계산 및 드롭
├── 난이도 조절
├── 몬스터 스폰
└── 경험치/골드 지급

PlayerController (1개 파일, ~800줄)
├── 이동/회전
├── 자동 공격
├── 업그레이드 효과 (switch문 22 case)
├── HP/데미지 처리
└── 모든 상태 직접 관리 (if-else chain)

Monster.cs (1개 파일)
└── Idle/Chase/Attack 상태를 if-else로 처리
```

**문제점:**
- 보상 확률 수정 → StageManager 전체를 다시 검토해야 함
- 새 업그레이드 추가 → switch 문에 case 추가 + 테스트 전체 재실행
- 새 몬스터 행동 추가 → Monster.Update()의 if-else 체인 수정
- 단위 테스트 불가: 보상 로직 테스트하려면 스테이지 전체를 시뮬레이션해야 함

---

### After: SOLID 구조

```
Manager 계층 (SRP)
├── StageManager (~200줄) - 진행만
├── RewardManager (~150줄) - 보상만
├── DifficultyManager (~40줄) - 난이도만
├── AchievementManager (~120줄) - 업적만
├── EquipmentManager (~260줄) - 장비만
└── SaveManager (~141줄) - 저장만

상태 머신 (OCP + DIP)
├── IMonsterState + 4개 구현체
└── IPetState + 4개 구현체

업그레이드 시스템 (OCP + DIP)
├── IUpgradeEffect + 6개 구현체
└── UpgradeEffectRegistry (switch 문 없음)

인터페이스 계층 (LSP + ISP)
├── IDamageable (Player/Monster 동일 처리)
├── IPoolable (Pool 생명주기)
└── 각 시스템 전용 인터페이스
```

**개선 결과:**

| 항목 | Before | After |
|------|--------|-------|
| 새 업그레이드 추가 | switch case 추가 + 기존 코드 수정 | 새 클래스 파일 1개 추가 |
| 새 몬스터 상태 추가 | Monster.Update() if-else 수정 | 새 클래스 파일 1개 추가 |
| 보상 비율 수정 | StageManager 전체 영향 분석 | RewardManager만 수정 |
| 난이도 공식 변경 | StageManager 내부 찾아서 수정 | DifficultyManager 1줄 수정 |
| 발사체 타겟 처리 | PlayerController/MonsterBase 분기 필요 | IDamageable 하나로 처리 |
| 파일당 평균 줄 수 | ~600줄 | ~120줄 |

---

## 9. 추가 디자인 패턴

SOLID 원칙 위에서 다음 패턴들이 함께 작동한다.

### Observer Pattern - 이벤트 기반 통신

```csharp
// EquipmentManager.cs
public event Action<EEquipSlot, EquipmentData> OnEquipChanged;

void Equip(EquipmentData item, EEquipSlot slot)
{
    _equipped[slot] = item;
    OnEquipChanged?.Invoke(slot, item);  // 구독자에게 브로드캐스트
}

// PetSpawner.cs - 구독
void OnEnable()
{
    EquipmentManager.Instance.OnEquipChanged += HandleEquipChanged;
}

void HandleEquipChanged(EEquipSlot slot, EquipmentData data)
{
    if (slot == EEquipSlot.Pet1 || slot == EEquipSlot.Pet2)
        RefreshPet(slot, data);
}
```

`EquipmentManager`는 `PetSpawner`의 존재를 모른다. 커플링 없이 시스템 간 통신.

---

### Object Pool Pattern - GC 최소화

```csharp
// ProjectileBase.cs - IPoolable 구현
public class ProjectileBase : MonoBehaviour, IPoolable
{
    public void OnPoolGet()
    {
        _hasHit = false;
        _hitMonsterIds.Clear();
        gameObject.SetActive(true);
    }

    public void OnPoolReturn()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    void ReturnToPool()
    {
        ObjectPool.Instance.Return(gameObject);  // Destroy 대신 재활용
    }
}
```

발사체를 Instantiate/Destroy 대신 재활용하여 GC 스파이크 방지.

---

### Generic Singleton - 코드 재사용

```csharp
// Manager/Singleton.cs
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    static T _instance;
    static bool _destroyed;

    public static T Instance
    {
        get
        {
            if (_destroyed) return null;
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }
}

// 사용 예
public class StageManager : Singleton<StageManager> { }
public class RewardManager : Singleton<RewardManager> { }
public class AchievementManager : Singleton<AchievementManager> { }
```

모든 Manager가 동일한 Singleton 인프라를 재사용.

---

### Factory Pattern - 생성 로직 캡슐화

```csharp
// ProjectileFactory.cs - 4가지 생성 시나리오를 하나의 팩토리에서 처리
public static class ProjectileFactory
{
    // 플레이어 투사체 (모든 스탯 포함)
    public static ProjectileBase Create(GameObject prefab, Vector3 pos, Vector3 dir, ProjectileInitData data)
    {
        var go = ObjectPool.Instance.Get(prefab);
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<ProjectileBase>();
        proj.Init(data);
        return proj;
    }

    // 몬스터 투사체 (데미지만)
    public static ProjectileBase Create(GameObject prefab, Vector3 pos, Vector3 dir, float damage) { ... }

    // 펫 투사체 (Arc 궤적)
    public static ProjectileBase CreateArc(GameObject prefab, Vector3 pos, Vector3 target, float damage) { ... }
}
```

호출자(PlayerController, MonsterBase, PetController)는 ObjectPool 관리, 위치/회전 설정, Init 호출 순서를 알 필요 없다.

---

### CSV-Driven Data - 외부 데이터 분리

```csharp
// UpgradeDatabase.cs, EquipmentDatabase.cs, AchievementDatabase.cs
// 동일한 패턴으로 CSV에서 런타임 데이터 로드

public static class EquipmentDatabase
{
    static Dictionary<int, EquipmentTable> _byId;

    static void Load()
    {
        var csv = Resources.Load<TextAsset>("Data/EquipmentData");
        // 헤더 기반 파싱 - 열 순서 변경에 강함
        string[] headers = lines[0].Split(',');
        foreach (var line in lines[1..])
        {
            var table = ParseRow(headers, line.Split(','));
            _byId[table.id] = table;
        }
    }
}
```

밸런스 데이터가 코드에 하드코딩되지 않는다. 기획자가 CSV 파일만 수정하면 된다.

---

*작성일: 2026-06-29*
*관련 문서: `docs/01_코어_아키텍처.md`, `docs/portfolio_refactoring.md`, `docs/14_FSM_설계.md`*
