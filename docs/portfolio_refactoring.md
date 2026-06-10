# Archer Game - Code Refactoring Portfolio

## Overview
Unity 기반 궁수의 전설 스타일 모바일 게임의 코드 품질, 아키텍처, 디자인 패턴을 전면 개선한 리팩토링 작업 기록.
면접관에게 **설계 능력, 클린 코드 역량, 성능 최적화 사고방식**을 증명하기 위한 포트폴리오 문서.

---

## 1. FindAnyObjectByType 제거 + 캐싱 패턴 적용 (Performance)

### 문제 (Before)
`FindAnyObjectByType`은 내부적으로 씬 전체 오브젝트를 순회하는 O(n) 연산이다.
이를 `Update()`, `OnEnable()` 등 매 프레임 호출되는 곳에서 사용하면 심각한 성능 저하를 일으킨다.

```csharp
// MonsterBase.cs - Update()에서 매 프레임 호출
if (_target == null)
{
    _playerController = FindAnyObjectByType<PlayerController>();
    if (_playerController != null)
        _target = _playerController.transform;
}
```

```csharp
// ProjectileBase.cs - OnEnable()마다 호출 (오브젝트 풀 재활용 시)
PlayerUpgrade upgrade = Object.FindAnyObjectByType<PlayerUpgrade>();
```

**영향 범위**: MonsterBase, StageManager, ExpManager, ProjectileBase, CameraController, UI_LevelUp, UI_PausePanel, HPHeart, ExpOrb, GoldOrb, EquipmentOrb 등 19개 파일

### 해결 (After)
**Static Instance 패턴**으로 PlayerController를 즉시 참조하도록 변경.

```csharp
// PlayerController.cs - Static Instance 추가
public class PlayerController : MonoBehaviour, IDamageable
{
    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        // ...
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
```

```csharp
// MonsterBase.cs - 캐싱된 인스턴스 사용
_playerController = PlayerController.Instance;
if (_playerController != null)
    _target = _playerController.transform;
```

```csharp
// ProjectileBase.cs - static 캐싱
PlayerUpgrade upgrade = PlayerController.Instance?.GetComponent<PlayerUpgrade>();
```

### 효과
- **성능**: O(n) 씬 순회 -> O(1) 직접 참조. 몬스터 50마리 기준 프레임당 ~50회의 불필요한 탐색 제거
- **일관성**: 모든 파일에서 동일한 참조 방식 사용
- **안전성**: `OnDestroy`에서 null 처리로 Dangling Reference 방지

---

## 2. StageManager 분리 - SRP 원칙 (Architecture)

### 문제 (Before)
`StageManager`가 573줄로 비대화. 스테이지 전환, 몬스터 스폰, 보상 드롭, 난이도 계산, 골드/장비 수집까지 모든 책임을 담당.

```csharp
// StageManager.cs - 보상 관련 코드만 200줄 이상
private List<ExpOrb> _spawnedExpOrbs = new List<ExpOrb>();
private List<HPHeart> _spawnedHpHearts = new List<HPHeart>();
private List<GoldOrb> _spawnedGoldOrbs = new List<GoldOrb>();
private List<EquipmentOrb> _spawnedEquipmentOrbs = new List<EquipmentOrb>();
// + SpawnExpOrb, SpawnHpHeart, SpawnGoldOrb, SpawnEquipmentOrb
// + CollectAllExpOrbs, CollectAllHpHearts, CollectAllGoldOrbs, CollectAllEquipmentOrbs
```

### 해결 (After)
**Single Responsibility Principle**에 따라 3개 매니저로 분리:

| 클래스 | 책임 |
|--------|------|
| `StageManager` | 스테이지 전환, 초기화, 몬스터 목록 관리 |
| `RewardManager` | ExpOrb, HPHeart, GoldOrb, EquipmentOrb 스폰/수집/정리 |
| `DifficultyManager` | 난이도 배율 계산, 부활 비용, 몬스터 스케일링 |

```csharp
// DifficultyManager.cs
public class DifficultyManager : Singleton<DifficultyManager>
{
    public float GetDifficultyMultiplier(int stageIndex)
    {
        return 1f + stageIndex * GameConfig.Instance.difficultyPerStage;
    }

    public int GetAliveCost(int stageIndex)
    {
        return stageIndex * GameConfig.Instance.aliveCostPerStage
             + GameConfig.Instance.aliveCostBase;
    }
}
```

```csharp
// StageManager.cs - 보상 처리를 RewardManager에 위임
public void OnMonsterDead(MonsterBase monster)
{
    _aliveMonsters.Remove(monster);
    RewardManager.Instance.DropMonsterRewards(monster, PlayerController.Instance);

    if (_aliveMonsters.Count == 0)
    {
        RewardManager.Instance.CollectAll();
        OpenExitDoor();
    }
}
```

### 효과
- **유지보수**: 보상 시스템 변경 시 RewardManager만 수정
- **확장성**: 새로운 보상 타입 추가 시 StageManager 코드 무변경
- **가독성**: StageManager 573줄 -> ~280줄 (51% 감소)

---

## 3. IDamageable, IPoolable 인터페이스 (Abstraction)

### 문제 (Before)
PlayerController와 MonsterBase가 동일한 HP/데미지 개념을 갖지만 공통 타입이 없어,
투사체가 타겟 타입을 직접 확인해야 했다.

```csharp
// ProjectileBase.cs - 타입별 분기
MonsterBase monster = other.GetComponent<MonsterBase>();
if (monster != null) { /* 몬스터 데미지 */ }
PlayerController player = other.GetComponent<PlayerController>();
if (player != null) { /* 플레이어 데미지 */ }
```

### 해결 (After)
```csharp
// Interfaces.cs
public interface IDamageable
{
    float MaxHp { get; }
    float CurrentHp { get; }
    void TakeDamage(float damage);
    void Heal(float amount);
    bool IsDead { get; }
    Transform transform { get; }
}

public interface IPoolable
{
    void OnPoolGet();
    void OnPoolReturn();
}
```

```csharp
// PlayerController, MonsterBase 모두 IDamageable 구현
public class PlayerController : MonoBehaviour, IDamageable { ... }
public abstract class MonsterBase : MonoBehaviour, IDamageable { ... }
```

### 효과
- **다형성**: 향후 IDamageable로 통합 데미지 시스템 구현 가능
- **테스트 용이**: Mock IDamageable로 단위 테스트 가능
- **확장성**: 파괴 가능한 오브젝트, NPC 등에도 동일 인터페이스 적용 가능

---

## 4. State Pattern - 몬스터 AI (Design Pattern)

### 문제 (Before)
MonsterBase와 서브클래스에서 `switch(State)` 또는 `if(State == ...)` 패턴으로 상태를 관리.
상태 추가 시 모든 서브클래스의 switch문을 수정해야 했다.

```csharp
// 각 서브클래스에서 반복되는 패턴
protected override void Update()
{
    base.Update();
    if (State == EState.Die) return;
    if (_isAttacking) return;
    // ... 상태별 로직
}
```

### 해결 (After)
State Pattern 인터페이스와 공통 상태 클래스를 제공하되, 서브클래스의 커스텀 로직은 그대로 유지.

```csharp
// IMonsterState.cs
public interface IMonsterState
{
    void Enter(MonsterBase monster);
    void Update(MonsterBase monster);
    void Exit(MonsterBase monster);
}

// MonsterIdleState.cs - Singleton 인스턴스로 GC 방지
public class MonsterIdleState : IMonsterState
{
    public static readonly MonsterIdleState Instance = new();

    public void Enter(MonsterBase monster)
    {
        monster.State = EState.Idle;
        monster.StopMovementPublic();
    }

    public void Update(MonsterBase monster)
    {
        if (monster.HasTarget)
            monster.TransitionTo(MonsterChaseState.Instance);
    }
}
```

```csharp
// MonsterBase.cs - 상태 전환 메서드
public void TransitionTo(IMonsterState newState)
{
    _currentState?.Exit(this);
    _currentState = newState;
    _currentState?.Enter(this);
}
```

### 효과
- **OCP**: 새로운 상태 추가 시 기존 코드 수정 불필요
- **가독성**: 상태별 로직이 독립 클래스로 분리
- **재사용**: `MonsterIdleState.Instance` 등 Singleton으로 메모리 효율적

---

## 5. Command Pattern - 업그레이드 시스템 (Design Pattern)

### 문제 (Before)
`PlayerController.ApplyUpgradeEffect()`에서 22가지 업그레이드를 switch문으로 처리.

```csharp
// PlayerController.cs
public void ApplyUpgradeEffect(EUpgradeType type)
{
    switch (type)
    {
        case EUpgradeType.WallPass:
            if (_passability != null) _passability.AddFlag(ETilePassFlag.WallPass);
            ApplyWallPassCollisions();
            break;
        case EUpgradeType.WaterWalker:
            // ...
        case EUpgradeType.Dwarf:
            // ...
        // 22 cases...
    }
}
```

### 해결 (After)
**Command Pattern**으로 각 업그레이드 효과를 독립 클래스로 분리하고 Dictionary로 등록.

```csharp
// IUpgradeEffect.cs
public interface IUpgradeEffect
{
    EUpgradeType Type { get; }
    void Apply(PlayerController player, PlayerUpgrade upgrade);
}

// UpgradeEffectRegistry.cs
public static class UpgradeEffectRegistry
{
    private static readonly Dictionary<EUpgradeType, IUpgradeEffect> _effects = new();

    private static void Init()
    {
        Register(new WallPassEffect());
        Register(new WaterWalkerEffect());
        Register(new DwarfEffect());
        // ...
    }

    public static void Apply(EUpgradeType type, PlayerController player, PlayerUpgrade upgrade)
    {
        Init();
        if (_effects.TryGetValue(type, out IUpgradeEffect effect))
            effect.Apply(player, upgrade);
    }
}

// WallPassEffect.cs
public class WallPassEffect : IUpgradeEffect
{
    public EUpgradeType Type => EUpgradeType.WallPass;
    public void Apply(PlayerController player, PlayerUpgrade upgrade)
    {
        TilePassability passability = player.GetComponent<TilePassability>();
        if (passability != null)
            passability.AddFlag(ETilePassFlag.WallPass);
        player.ApplyWallPassCollisions();
    }
}
```

```csharp
// PlayerController.cs - 1줄로 대체
public void ApplyUpgradeEffect(EUpgradeType type)
{
    UpgradeEffectRegistry.Apply(type, this, _upgrade);
}
```

### 효과
- **OCP**: 새 업그레이드 추가 시 Effect 클래스 1개 + Register 1줄만 추가
- **SRP**: 각 효과의 로직이 PlayerController에서 완전히 분리
- **테스트**: 개별 효과를 독립적으로 테스트 가능

---

## 6. Factory Pattern - 투사체 생성 (Design Pattern)

### 문제 (Before)
투사체 생성 로직이 PlayerController, ArcherMonster, GolemMonster 등 여러 곳에 중복.

```csharp
// PlayerController.cs
GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
go.transform.position = spawnPos;
go.transform.rotation = Quaternion.LookRotation(dir);
ProjectileBase proj = go.GetComponent<ProjectileBase>();
if (proj != null) proj.Init(data);
```

### 해결 (After)
```csharp
// ProjectileFactory.cs
public static class ProjectileFactory
{
    public static ProjectileBase Create(GameObject prefab, Vector3 position,
        Vector3 direction, ProjectileInitData data)
    {
        if (prefab == null) return null;
        GameObject go = ObjectPool.Instance.Get(prefab);
        go.transform.position = position;
        go.transform.rotation = Quaternion.LookRotation(direction);
        ProjectileBase proj = go.GetComponent<ProjectileBase>();
        if (proj != null) proj.Init(data);
        return proj;
    }

    // 몬스터 발사체용 오버로드
    public static ProjectileBase Create(GameObject prefab, Vector3 position,
        Vector3 direction, float damage) { ... }

    // Arc 투사체용
    public static ProjectileBase CreateArc(GameObject prefab, Vector3 position,
        Vector3 targetPos, float damage) { ... }
}
```

```csharp
// PlayerController.cs - 팩토리 호출로 대체
ProjectileFactory.Create(_projectilePrefab, spawnPos, dir, data);
```

### 효과
- **DRY**: 투사체 생성 로직 중복 제거
- **단일 수정점**: 풀링 방식 변경 시 Factory만 수정
- **일관성**: 모든 투사체가 동일한 경로로 생성/초기화

---

## 7. GameConfig ScriptableObject - 매직 넘버 제거 (Code Quality)

### 문제 (Before)
게임 밸런스 수치가 코드 곳곳에 하드코딩되어 있어, 수정 시 코드를 직접 변경해야 했다.

```csharp
float difficultyMult = 1f + _currentStageIndex * 0.15f;  // StageManager
public int AliveCost => _currentStageIndex * 50 + 100;     // StageManager
public int ExpToNextLevel => 50 + (_level * 30);            // ExpManager
private const float PATH_REFRESH_INTERVAL = 0.5f;          // MonsterBase
private const float FOOTSTEP_INTERVAL = 0.3f;              // PlayerController
```

### 해결 (After)
```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Data/GameConfig")]
public class GameConfig : ScriptableObject
{
    private static GameConfig _instance;
    public static GameConfig Instance { get { ... } }

    [Header("Difficulty Scaling")]
    public float difficultyPerStage = 0.15f;

    [Header("Experience")]
    public int baseExpRequired = 50;
    public int expPerLevel = 30;

    [Header("Alive Cost")]
    public int aliveCostBase = 100;
    public int aliveCostPerStage = 50;

    [Header("Monster AI")]
    public float pathRefreshInterval = 0.5f;

    [Header("Player")]
    public float footstepInterval = 0.3f;
}
```

### 효과
- **디자이너 친화**: Inspector에서 밸런스 수치 조정 가능
- **빌드 불필요**: ScriptableObject 수정은 코드 재컴파일 없이 적용
- **중앙 관리**: 모든 밸런스 수치가 한 곳에 집중

---

## 8. Save 시스템 보강 - 데이터 무결성 (Robustness)

### 문제 (Before)
```csharp
public static void Save()
{
    SaveData data = EquipmentManager.Instance.ToSaveData();
    string json = JsonUtility.ToJson(data, true);
    System.IO.File.WriteAllText(SavePath, json);  // 예외 처리 없음
}
```
- 파일 I/O 예외 시 크래시 발생
- 데이터 변조 감지 불가
- 버전 호환성 처리 없음

### 해결 (After)
```csharp
public static void Save()
{
    try
    {
        SaveData data = EquipmentManager.Instance.ToSaveData();
        data.version = CURRENT_VERSION;
        data.checksum = "";
        string jsonForHash = JsonUtility.ToJson(data, false);
        data.checksum = ComputeChecksum(jsonForHash);
        string json = JsonUtility.ToJson(data, true);
        System.IO.File.WriteAllText(SavePath, json);
    }
    catch (System.Exception e)
    {
        Logger.LogError("SaveManager", $"Save failed: {e.Message}");
    }
}

private static string ComputeChecksum(string input)
{
    using (SHA256 sha256 = SHA256.Create())
    {
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        // ...
    }
}

private static void MigrateIfNeeded(SaveData data)
{
    if (data.version < CURRENT_VERSION)
    {
        Logger.Log("SaveManager", $"Migrating save v{data.version} -> v{CURRENT_VERSION}");
        data.version = CURRENT_VERSION;
    }
}
```

### 효과
- **안정성**: try-catch로 I/O 실패 시에도 게임 크래시 방지
- **무결성**: SHA256 체크섬으로 데이터 변조 감지
- **호환성**: 버전 마이그레이션으로 업데이트 시 기존 세이브 유지

---

## 9. 에러 처리 강화 (Robustness)

### 문제 (Before)
- Resources.Load 실패 시 NullReferenceException
- CSV 파싱 실패 시 전체 데이터 로드 실패
- 에디터 로그가 빌드에도 포함되어 성능 저하

### 해결 (After)

**GameManager - 리소스 로드 에러 처리:**
```csharp
private void LoadResource<T>(string path) where T : Object
{
    T resource = Resources.Load<T>(path);
    if (resource == null)
        Logger.LogWarning("GameManager", $"Failed to preload resource: {path}");
}
```

**UpgradeDatabase/EquipmentDatabase - CSV 파싱 방어:**
```csharp
try
{
    var info = new UpgradeInfo { type = System.Enum.Parse<EUpgradeType>(...) };
    list.Add(info);
}
catch (System.Exception e)
{
    Logger.LogWarning("UpgradeDatabase", $"Failed to parse row {i}: {e.Message}");
    continue;  // 해당 행만 스킵, 나머지는 정상 로드
}
```

**커스텀 로거:**
```csharp
public static class Logger
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string tag, string message)
    {
        Debug.Log($"[{tag}] {message}");
    }
}
```

### 효과
- **빌드 성능**: `[Conditional]` 어트리뷰트로 릴리즈 빌드에서 로그 호출 완전 제거
- **부분 실패 허용**: CSV 1행 오류가 전체 테이블 로드를 차단하지 않음
- **태그 기반 추적**: `[SaveManager]`, `[GameManager]` 등으로 로그 필터링 용이

---

## 10. 오디오 Fade / 커스텀 로거 (Polish)

### BGM Fade In/Out
```csharp
public void FadeBGMOut(float duration = -1f)
{
    if (duration < 0f) duration = GameConfig.Instance.bgmFadeDuration;
    StartCoroutine(FadeBGMCoroutine(_bgmSource.volume, 0f, duration));
}

public void CrossFadeBGM(AudioClip newClip, float duration = -1f)
{
    StartCoroutine(CrossFadeBGMCoroutine(newClip, duration));
}
```
- BGM 전환 시 끊김 없는 크로스페이드
- `Time.unscaledDeltaTime` 사용으로 일시정지 중에도 동작

---

## 11. 성능 최적화 (Performance)

### FindClosestMonster 캐싱
```csharp
// Before: 매 프레임 전체 몬스터 순회
private MonsterBase FindClosestMonster() { ... }

// After: 0.1초 주기로 캐싱
private MonsterBase _cachedClosestMonster;
private float _closestMonsterCacheTimer;

private MonsterBase FindClosestMonster()
{
    _closestMonsterCacheTimer -= Time.deltaTime;
    if (_closestMonsterCacheTimer <= 0f)
    {
        _closestMonsterCacheTimer = GameConfig.Instance.closestMonsterCacheInterval;
        _cachedClosestMonster = FindClosestMonsterInternal();
    }
    // 캐시된 몬스터가 죽으면 즉시 재탐색
    if (_cachedClosestMonster == null || _cachedClosestMonster.CurrentHp <= 0)
        _cachedClosestMonster = FindClosestMonsterInternal();
    return _cachedClosestMonster;
}
```
- 60FPS 기준 프레임당 1회 -> 0.1초당 1회 = **~83% 호출 감소**
- 타겟 사망 시 즉시 재탐색으로 반응성 유지

### ProjectileBase 중복 제거
```csharp
// Before: 무의미한 position 재할당
transform.position += _velocity * dt;
transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);

// After: 중복 제거
transform.position += _velocity * dt;
```

---

## 12. 버그 수정 (Bug Fixes)

### Y좌표 강제 고정
```csharp
// Before: 매 프레임 무조건 Y=0 (인트로 낙하 중에도 실행됨)
if (!IsIntroDrop)
    transform.position = new Vector3(transform.position.x, 0, transform.position.z);

// After: Y가 0이 아닐 때만 보정 (조건부)
if (transform.position.y != 0f)
    transform.position = new Vector3(transform.position.x, 0, transform.position.z);
```

### Legendary 등급 스프라이트
```csharp
// Before: TODO 방치
EEquipGrade.Legendary => "GradeBg_Epic", // TODO: Legendary 전용 스프라이트 추가 시 변경

// After: 의도적인 fallback임을 명시
EEquipGrade.Legendary => "GradeBg_Epic", // Legendary: Epic 배경을 공유 (별도 스프라이트 없음)
```

---

## Summary

| 카테고리 | 변경 항목 | 적용 원칙/패턴 |
|----------|----------|--------------|
| 성능 | FindAnyObjectByType 제거 (19개 파일) | 캐싱, Static Instance |
| 아키텍처 | StageManager 분리 | SRP (단일 책임 원칙) |
| 추상화 | IDamageable, IPoolable | Interface Segregation |
| 디자인 패턴 | 몬스터 상태 관리 | State Pattern |
| 디자인 패턴 | 업그레이드 효과 | Command Pattern |
| 디자인 패턴 | 투사체 생성 | Factory Pattern |
| 코드 품질 | 매직 넘버 제거 | ScriptableObject |
| 안정성 | 세이브 시스템 | try-catch, Checksum, Migration |
| 안정성 | 에러 처리 강화 | Graceful Degradation |
| 완성도 | BGM Fade | Coroutine |
| 완성도 | 커스텀 로거 | Conditional Compilation |
| 성능 | FindClosestMonster 캐싱 | Temporal Caching |

### 신규 파일 (13개)
- `Interfaces.cs` - IDamageable, IPoolable
- `GameConfig.cs` - 밸런스 중앙 관리
- `Logger.cs` - 커스텀 로거
- `IMonsterState.cs` + 4개 상태 클래스 - State Pattern
- `IUpgradeEffect.cs` + `UpgradeEffectRegistry.cs` - Command Pattern
- `ProjectileFactory.cs` - Factory Pattern
- `RewardManager.cs` - 보상 관리
- `DifficultyManager.cs` - 난이도 관리

### 수정 파일 (19개)
MonsterBase, PlayerController, StageManager, SaveManager, AudioManager, GameManager, ExpManager, ProjectileBase, CameraController, Define, UpgradeDatabase, EquipmentDatabase, UIManager, UI_LevelUp, UI_PausePanel, HPHeart, ExpOrb, GoldOrb, EquipmentOrb, UI_AngelPanel, UI_EquipmentPanel, UI_ItemDetailPopup
