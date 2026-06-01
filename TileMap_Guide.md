# TileMap 시스템 종합 가이드

## 1. 개요

TileMap 시스템은 그리드 기반 맵을 런타임에 생성하고, 타일 타입별 통행 규칙을 적용하며, A* 길찾기를 통해 몬스터 AI의 경로탐색을 지원하는 아키텍처이다.

### 아키텍처 요약

```
[TileMap.cs]         — 맵 데이터 보유, 시각 생성, 통행 판정 API
[TilePassability.cs] — 엔티티별 통행 플래그 컴포넌트
[AStarPathfinder.cs] — 정적 A* 길찾기 유틸리티
[MonsterBase.cs]     — 경로 갱신/추종 로직 (RefreshPath, GetPathDirection)
[TileMapEditor.cs]   — 에디터 브러시 도구
[SpikeTile.cs]       — 가시 데미지 컴포넌트
[BlockObstacle.cs]   — 장애물 마커 컴포넌트
[WaterObstacle.cs]   — 물 타일 마커 컴포넌트
```

### 관련 파일 목록

| 파일 경로 | 역할 |
|---|---|
| `Assets/Scripts/Map/TileMap.cs` | 핵심 맵 컴포넌트 |
| `Assets/Scripts/Map/TilePassability.cs` | 엔티티 통과 플래그 |
| `Assets/Scripts/Map/SpikeTile.cs` | 가시 데미지 로직 |
| `Assets/Scripts/Map/BlockObstacle.cs` | 블록 장애물 마커 |
| `Assets/Scripts/Map/WaterObstacle.cs` | 물 타일 마커 |
| `Assets/Scripts/Map/Editor/TileMapEditor.cs` | 커스텀 에디터 |
| `Assets/Scripts/Utils/AStarPathfinder.cs` | A* 알고리즘 |
| `Assets/Scripts/Utils/Define.cs` | ETileType, ETilePassFlag 정의 |
| `Assets/Scripts/Controllers/MonsterBase.cs` | 몬스터 기본 클래스 |

---

## 2. 데이터 구조

### ETileType 열거형 (`Define.cs:5`)

```csharp
public enum ETileType
{
    Path  = 0,  // 기본 이동 가능 타일
    Wall  = 1,  // 벽 — WallPass만 통과
    Water = 2,  // 물 — Fly 또는 WaterWalk만 통과
    Spike = 3,  // 가시 — 이동 가능하나 데미지 발생
}
```

### ETilePassFlag 플래그 열거형 (`Define.cs:13`)

```csharp
[System.Flags]
public enum ETilePassFlag
{
    None      = 0,
    Walk      = 1 << 0,  // Path, Spike 통과
    Fly       = 1 << 1,  // Path, Spike, Water 통과
    WaterWalk = 1 << 2,  // Water 통과
    WallPass  = 1 << 3,  // Wall, Block 통과
}
```

### 직렬화 구조체 (`TileMap.cs:6-24`)

```csharp
[System.Serializable]
public struct TileEntry
{
    public Vector2Int pos;   // 그리드 좌표
    public ETileType type;   // 타일 타입
}

[System.Serializable]
public struct BlockEntry
{
    public Vector2Int pos;       // 그리드 좌표
    public int prefabIndex;      // _blockPrefabs 배열 인덱스
}

[System.Serializable]
public struct MonsterEntry
{
    public Vector2Int pos;       // 그리드 좌표
    public int prefabIndex;      // _monsterPrefabs 배열 인덱스
}
```

### Dictionary 캐싱 시스템 (`TileMap.cs:84-93`)

`GenerateVisuals()` 호출 시 `BuildCache()`가 실행되어 리스트를 Dictionary로 변환한다.

```csharp
private void BuildCache()
{
    _tileCache = new Dictionary<Vector2Int, ETileType>(_tileOverrides.Count);
    foreach (var e in _tileOverrides)
        _tileCache[e.pos] = e.type;

    _blockCache = new Dictionary<Vector2Int, int>(_placedBlocks.Count);
    foreach (var b in _placedBlocks)
        _blockCache[b.pos] = b.prefabIndex;
}
```

**설계 의도:**
- `_tileOverrides` 리스트는 직렬화에 적합하지만 조회가 O(n)
- 런타임에는 Dictionary로 O(1) 조회
- 에디터에서는 캐시 없이 폴백으로 리스트 순회 (호환성 보장)

---

## 3. 좌표 변환

### WorldToGrid (`TileMap.cs:98-103`)

월드 좌표를 그리드 정수 좌표로 변환한다. `0.5f` 바이어스를 더해 `FloorToInt`로 반올림하여 타일 중심 기준 가장 가까운 셀을 반환한다.

```csharp
public Vector2Int WorldToGrid(Vector3 worldPos)
{
    int x = Mathf.FloorToInt(worldPos.x - _origin.x + 0.5f);
    int z = Mathf.FloorToInt(worldPos.z - _origin.z + 0.5f);
    return new Vector2Int(x, z);
}
```

**동작 원리:**
- `_origin`은 그리드 (0,0) 타일의 월드 좌표
- 타일 크기는 1×1 (단위 정수 그리드)
- `+0.5f` 바이어스로 인해 타일 중심에서 ±0.5 범위가 같은 셀에 매핑

### GridToWorld (`TileMap.cs:108-111`)

그리드 좌표를 타일 중심의 월드 좌표로 역변환한다.

```csharp
public Vector3 GridToWorld(int x, int z)
{
    return new Vector3(_origin.x + x, _origin.y, _origin.z + z);
}
```

---

## 4. 맵 시각화 생성 (`GenerateVisuals()`)

`TileMap.cs:204-293` — 전체 맵의 3D 오브젝트를 절차적으로 생성한다.

### 생성 흐름

```
1. ClearVisuals()         — 기존 시각 오브젝트 제거
2. BuildCache()           — Dictionary 캐시 구축
3. 컨테이너 생성          — "_TileVisuals" GameObject
4. 타일 순회 (width × height):
   a. Floor Cube 생성 (개별 BoxCollider 제거)
   b. Material 할당 (타입별)
   c. Spike 프리팹 배치
   d. Block 프리팹 배치 + BlockObstacle 컴포넌트
   e. Wall/Water 위치 수집
5. CreateFloorCollider    — 단일 바닥 콜라이더
6. CreateMergedColliders  — Wall/Water 병합 콜라이더
7. ExitDoor 배치
8. AddBoundaryColliders   — 4면 경계벽
9. GenerateBackgroundBlocks — 외곽 배경 블록
10. StaticBatchingUtility.Combine() — Static Batching
```

### Floor Cube 생성

```csharp
GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
floor.transform.position = tileCenter + Vector3.down * (_floorThickness / 2f);
floor.transform.localScale = new Vector3(1f, _floorThickness, 1f);
floor.isStatic = true;

// 개별 BoxCollider 제거 (단일 FloorCollider로 대체)
Object.DestroyImmediate(floor.GetComponent<BoxCollider>());
```

- `CreatePrimitive(Cube)`는 자동으로 BoxCollider를 추가하지만, 성능을 위해 즉시 제거
- 물리는 단일 `CreateFloorCollider`가 담당

### 머티리얼 할당 (`GetMaterialForType`, `TileMap.cs:461-480`)

| 타일 타입 | 머티리얼 로직 |
|---|---|
| Path | `_pathMaterials[(x + z) % length]` — 체커보드 패턴 |
| Wall | `_wallMaterial` — 단일 머티리얼 |
| Water | `GetWaterMaterial(x, z)` — 5종 엣지 감지 |
| Spike | Path와 동일한 체커보드 (가시는 프리팹으로 분리 표시) |

---

## 5. 타일 타입별 상세

### Path (경로)

- 기본 이동 가능 타일
- `Walk` 또는 `Fly` 플래그로 통과 가능
- 체커보드 패턴: `(x + z) % N` 인덱스로 머티리얼 순환 배치

### Wall (벽)

- 기본적으로 이동 불가
- `WallPass` 플래그가 있어야만 통과 가능
- 맵 범위 밖(`x < 0 || x >= width || z < 0 || z >= height`)은 자동으로 Wall 취급

### Water (물)

- `Fly` 또는 `WaterWalk` 플래그로만 통과 가능
- 5종 엣지 머티리얼 시스템 (`GetWaterMaterial`, `TileMap.cs:486-503`):

```csharp
private Material GetWaterMaterial(int x, int z)
{
    bool n = GetTile(x, z + 1) == ETileType.Water;
    bool s = GetTile(x, z - 1) == ETileType.Water;
    bool w = GetTile(x - 1, z) == ETileType.Water;
    bool e = GetTile(x + 1, z) == ETileType.Water;
    int count = (n?1:0) + (s?1:0) + (w?1:0) + (e?1:0);

    if (count >= 2) return _waterMaterials[4]; // Center
    if (s && !n && !w && !e) return _waterMaterials[0]; // North Edge
    if (n && !s && !w && !e) return _waterMaterials[1]; // South Edge
    if (e && !n && !s && !w) return _waterMaterials[2]; // West Edge
    if (w && !n && !s && !e) return _waterMaterials[3]; // East Edge
    return _waterMaterials[4]; // Isolated → Center
}
```

**엣지 판정 로직:**
- 인접 타일 중 Water인 방향을 감지
- 인접 Water가 2개 이상이면 Center
- 인접 Water가 1개면 해당 방향의 반대쪽이 "엣지"
- 예: 남쪽에만 Water가 있으면 → 현재 타일은 North Edge (물웅덩이의 북쪽 끝)

### Spike (가시)

- 이동 자체는 가능 (`Walk` 또는 `Fly`)
- `SpikeTile` 컴포넌트가 데미지를 처리

**SpikeTile 데미지 메커니즘 (`SpikeTile.cs`):**

| 상태 | 데미지 | 인터벌 | 타이머 초기화 |
|---|---|---|---|
| 이동 중 (Move) | 20 | 1.5초 | exit 후에도 지속 |
| 정지 중 (Idle) | 5 | 0.5초 | exit 시 리셋 |

```csharp
// 이동 중 기본 데미지
if (player.State == EState.Move && _baseDamageTimer <= 0f)
{
    player.TakeDamage(_baseDamage);        // 20
    _baseDamageTimer = _baseDamageInterval; // 1.5s
}

// 정지 중 지속 데미지
_continuousDamageTimer -= Time.deltaTime;
if (_continuousDamageTimer <= 0f)
{
    player.TakeDamage(_continuousDamage);              // 5
    _continuousDamageTimer = _continuousDamageInterval; // 0.5s
}
```

**핵심 설계:**
- `_baseDamageTimer`는 전역적으로 흐름 → 빠르게 연속 진입해도 쿨다운 적용
- `_continuousDamageTimer`는 exit 시 리셋 → 재진입 시 즉시 데미지 없음
- 이동 중이면 `_continuousDamageTimer`를 0으로 초기화 → 멈추면 즉시 지속 데미지 시작

---

## 6. 장애물 시스템

### BlockObstacle (`BlockObstacle.cs`)

```csharp
public class BlockObstacle : MonoBehaviour
{
    public bool IsBoundary { get; set; }
}
```

- 프로젝타일을 차단하는 마커 컴포넌트
- `IsBoundary = true`인 경우 WallPass로도 통과 불가 (4면 경계벽)
- Block이 있는 타일은 `CanPassBlock()`이 false 반환 → A*에서 회피

### WaterObstacle (`WaterObstacle.cs`)

```csharp
public class WaterObstacle : MonoBehaviour { }
```

- 물 타일 콜라이더의 마커 컴포넌트
- WaterWalker 업그레이드 시 이 컴포넌트가 붙은 콜라이더를 trigger로 전환하여 통과 허용

### 업그레이드 연동

| 업그레이드 | 대상 컴포넌트 | 전환 동작 |
|---|---|---|
| WallPass | `BlockObstacle` | collider → `isTrigger = true` |
| WaterWalker | `WaterObstacle` | collider → `isTrigger = true` |

플레이어의 `TilePassability` 플래그에 해당 플래그를 추가하고, 물리 콜라이더를 트리거로 전환하여 실제 통과가 가능해진다.

---

## 7. 통행 판정 API

### GetTile (`TileMap.cs:116-132`)

```csharp
public ETileType GetTile(int x, int z)
{
    if (x < 0 || x >= _width || z < 0 || z >= _height) return ETileType.Wall;

    if (_tileCache != null)
    {
        if (_tileCache.TryGetValue(new Vector2Int(x, z), out ETileType type))
            return type;
        return ETileType.Path;  // 기본값
    }

    // 에디터 폴백: 리스트 순회
    foreach (var e in _tileOverrides)
        if (e.pos.x == x && e.pos.y == z) return e.type;
    return ETileType.Path;
}
```

**특징:**
- 범위 밖 → Wall (자동 차단)
- `_tileOverrides`에 없는 좌표 → Path (기본값)
- 즉, 오버라이드된 타일만 저장하는 sparse 방식

### CanPassBlock (`TileMap.cs:153`)

```csharp
public bool CanPassBlock(int x, int z) => GetBlockIndex(x, z) < 0;
```

해당 좌표에 Block 프리팹이 없으면 true. A* 경로탐색에서 직접 사용.

### CanPass (`TileMap.cs:159-171`)

종합 통행 판정 — 경계, 타일 타입, 블록을 모두 확인한다.

```csharp
public bool CanPass(Vector3 worldPos, ETilePassFlag flags)
{
    if (_width == 0 || _height == 0) return true;  // 맵 없음 → 통과

    Vector2Int grid = WorldToGrid(worldPos);
    if (grid.x < 0 || grid.x >= _width || grid.y < 0 || grid.y >= _height)
        return false;  // 경계 밖 → 차단

    ETileType tileType = GetTile(grid.x, grid.y);
    if (!IsTilePassable(tileType, flags)) return false;  // 타일 불통과

    if (GetBlockIndex(grid.x, grid.y) >= 0)
        return (flags & ETilePassFlag.WallPass) != 0;  // 블록 → WallPass만 통과

    return true;
}
```

**판정 순서:**
1. 맵 미설정 → 무조건 통과 (하위 호환)
2. 경계 밖 → 무조건 차단
3. 타일 타입 검사 (`IsTilePassable`)
4. 블록 존재 시 → `WallPass` 필요

### IsTilePassable (`TileMap.cs:176-191`)

타일 타입 × 패스 플래그 매트릭스:

| 타일 타입 | 통과 가능 플래그 |
|---|---|
| Path | Walk \| Fly |
| Wall | WallPass |
| Water | Fly \| WaterWalk |
| Spike | Walk \| Fly |

```csharp
public static bool IsTilePassable(ETileType tileType, ETilePassFlag flags)
{
    switch (tileType)
    {
        case ETileType.Path:  return (flags & (ETilePassFlag.Walk | ETilePassFlag.Fly)) != 0;
        case ETileType.Wall:  return (flags & ETilePassFlag.WallPass) != 0;
        case ETileType.Water: return (flags & (ETilePassFlag.Fly | ETilePassFlag.WaterWalk)) != 0;
        case ETileType.Spike: return (flags & (ETilePassFlag.Walk | ETilePassFlag.Fly)) != 0;
        default: return false;
    }
}
```

---

## 8. A* 길찾기 (`AStarPathfinder.cs`)

### 알고리즘 개요

`AStarPathfinder`는 정적 클래스로, TileMap 그리드 위에서 4방향 A* 탐색을 수행한다.

### 핵심 구조

```csharp
private struct Node
{
    public Vector2Int pos;
    public int gCost;         // 시작점에서의 실제 비용
    public int hCost;         // 목표까지 휴리스틱 추정
    public int fCost => gCost + hCost;
    public Vector2Int parent; // 역추적용 부모 노드
}
```

### Open Set 구현

```csharp
SortedSet<(int fCost, int hCost, int posX, int posY)> openSet = new();
```

- `SortedSet`은 자동 정렬 → `Min`으로 최소 f-cost 노드를 O(log n) 추출
- 튜플 비교: fCost → hCost → posX → posY 순으로 정렬 (동점 처리)

### 휴리스틱

Manhattan 거리 (4방향 이동이므로 admissible):

```csharp
private static int ManhattanDist(Vector2Int a, Vector2Int b)
    => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
```

### IsWalkable — 이중 검사 (`AStarPathfinder.cs:94-102`)

```csharp
private static bool IsWalkable(TileMap tileMap, Vector2Int pos, ETilePassFlag flags)
{
    ETileType tile = tileMap.GetTile(pos.x, pos.y);
    if (!TileMap.IsTilePassable(tile, flags))
        return false;
    if (!tileMap.CanPassBlock(pos.x, pos.y))
        return false;
    return true;
}
```

- 타일 타입 검사 + 블록 존재 검사를 모두 수행
- `CanPass()`와 유사하지만 좌표 변환 없이 직접 그리드 좌표 사용

### 도착지 불가 처리 (`AStarPathfinder.cs:36-40`)

```csharp
if (!IsWalkable(tileMap, to, passFlags))
{
    Vector2Int? alt = FindNearestWalkable(tileMap, to, passFlags);
    if (alt == null) return new List<Vector2Int>();
    to = alt.Value;
}
```

목표 타일이 이동 불가하면 인접 4방향 중 이동 가능한 타일로 대체한다.

### 경로 재구성 (`AStarPathfinder.cs:118-129`)

```csharp
private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Node> nodes,
                                                 Vector2Int from, Vector2Int to)
{
    List<Vector2Int> path = new();
    Vector2Int cur = to;
    while (cur != from)
    {
        path.Add(cur);
        cur = nodes[cur].parent;
    }
    path.Reverse();
    return path;
}
```

- `to`에서 `parent`를 따라 역추적
- `from`은 제외, `to`는 포함
- 최종 `Reverse()`로 시작→끝 순서 정렬

### 전체 FindPath 호출 흐름

```
FindPath(tileMap, from, to, passFlags)
  │
  ├─ from == to → 빈 리스트 반환
  ├─ 목표 불가 → FindNearestWalkable → 대체 또는 실패
  │
  ├─ Open Set에 시작 노드 추가
  │
  └─ while (openSet.Count > 0):
       ├─ 최소 fCost 노드 추출
       ├─ 목표 도달 → ReconstructPath
       └─ 4방향 탐색:
            ├─ 범위 확인
            ├─ IsWalkable 확인
            ├─ gCost 비교 (더 짧은 경로만 갱신)
            └─ openSet에 추가/갱신
```

---

## 9. 몬스터 경로탐색

### MonsterBase 경로 시스템 (`MonsterBase.cs:249-303`)

#### RefreshPath — 0.5초 간격 갱신

```csharp
protected void RefreshPath()
{
    _pathRefreshTimer -= Time.deltaTime;
    if (_pathRefreshTimer > 0f && _path.Count > 0)
        return;

    _pathRefreshTimer = PATH_REFRESH_INTERVAL; // 0.5s
    TileMap tileMap = StageManager.Instance.TileMap;

    Vector2Int myGrid = tileMap.WorldToGrid(transform.position);
    Vector2Int targetGrid = tileMap.WorldToGrid(_target.position);
    _path = AStarPathfinder.FindPath(tileMap, myGrid, targetGrid, _passability.PassFlags);
    _pathIndex = 0;
}
```

#### GetPathDirection — 웨이포인트 0.3f 임계값

```csharp
protected Vector3 GetPathDirection()
{
    // 경로 없으면 직선 방향 fallback
    if (tileMap == null || _path.Count == 0 || _pathIndex >= _path.Count)
    {
        Vector3 direct = (_target.position - transform.position).normalized;
        direct.y = 0;
        return direct;
    }

    Vector3 waypointWorld = tileMap.GridToWorld(_path[_pathIndex].x, _path[_pathIndex].y);
    Vector3 toWaypoint = waypointWorld - transform.position;
    toWaypoint.y = 0;

    // 웨이포인트에 0.3 이내로 접근하면 다음으로
    if (toWaypoint.magnitude < 0.3f)
    {
        _pathIndex++;
        // 다음 웨이포인트 방향 계산...
    }

    return toWaypoint.normalized;
}
```

### 몬스터별 이동 방식 표

| 몬스터 | A* 사용 | 통과 플래그 | 이동 방식 |
|---|---|---|---|
| **SkeletonMonster** | O | Walk | 지속 A* 추적 |
| **BatMonster** | O (추적 중) | Fly | A* 추적 + 주기적 차지 돌진 |
| **GhostMonster** | X | Walk | 배회 + 포물선 점프 (플레이어 방향) |
| **GolemMonster** | X | Walk | 스폰 기준 배회 + 360도 회전 공격 |
| **ArcherMonster** | X | Walk | 완전 고정 — 반사 레이저 + 화살 |
| **ImpMischief** | O (추적 중) | Walk | A* 추적 + 3종 스킬 순환 |
| **TreantMinion** | X | Walk | 4방향 포물선 점프 + 착지 시 4방향 발사 |
| **SplitterMonster** | O | Walk | A* 추적 + 사망 시 분열 |
| **Nepenthes** | X | — (immovable) | 완전 고정 — 산탄 발사 |

### 몬스터별 상세

#### SkeletonMonster
- 매 프레임 `RefreshPath()` → `GetPathDirection()` → `MoveToward()`
- 가장 단순한 A* 추적 패턴

#### BatMonster
- **추적 단계**: A* + Fly 플래그 (벽/물 위 통과)
- **차지 단계**: 2~5초 랜덤 타이머 후 플레이어 방향 고정, `_chargeSpeed`(12) 직선 돌진
- 차지 중 장애물 충돌 시 중단

#### GhostMonster
- A* 미사용 — 물리적 점프로 이동
- 배회: 홈 포지션 주변 `_wanderRadius`(4) 내 랜덤 이동
- 점프: 1.5~3초 간격, Sin 포물선 궤적, 착지점 `CanMoveTo()` 검증 (최대 10회 재시도)
- 플레이어가 `_chaseRange`(8) 내면 플레이어 방향 점프

#### GolemMonster
- A* 미사용 — 스폰 위치 기준 `_patrolRadius`(4) 내 배회
- 이동 경로 3개 중간점 `CanMoveTo()` 검증
- 공격: 3~7초 간격, 360도 회전(1초) 후 3방향(0°, +45°, -45°) 발사체

#### ArcherMonster
- 완전 고정 — `FaceTarget()`으로 플레이어만 바라봄
- 1.5초 경고 단계: `LineRenderer`로 반사 경로 실시간 표시 (BlockObstacle 반사)
- 이후 반사 화살 발사

#### ImpMischief (보스)
- **추적**: A* + `_chaseSpeed`(2.5), 2~4초 지속
- **스킬 사이클** (Fisher-Yates 셔플):
  - Skill 1 (Fireball): 정지 후 3발 (0°/±20° 확산)
  - Skill 2 (Headbutt): 방향 고정, `OverlapSphere` 전방 스윕
  - Skill 3 (Roll): `_rollSpeed`(18)로 `_rollMaxDistance`(10) 돌진, `CanMoveTo()` 차단

#### TreantMinion
- A* 미사용 — 축 정렬 점프
- 2~4초 대기 → 플레이어 방향과 dot product가 가장 큰 축 방향 선택
- Sin 포물선 점프, 착지점 불가 시 다른 방향 시도
- 착지 시 4방향(상하좌우) 동시 발사체

---

## 10. 물리 최적화

### 단일 Floor Collider (`TileMap.cs:298-307`)

```csharp
private void CreateFloorCollider(Transform container)
{
    GameObject floorCol = new GameObject("FloorCollider");
    floorCol.transform.SetParent(container);
    float cx = _origin.x + _width / 2f - 0.5f;
    float cz = _origin.z + _height / 2f - 0.5f;
    floorCol.transform.position = new Vector3(cx, _origin.y - _floorThickness / 2f, cz);
    BoxCollider bc = floorCol.AddComponent<BoxCollider>();
    bc.size = new Vector3(_width, _floorThickness, _height);
}
```

- 타일 수 × BoxCollider 대신 **단일** BoxCollider로 전체 바닥 커버
- 타일별 개별 Collider는 `DestroyImmediate`로 제거

### Greedy Rectangle Merge (`TileMap.cs:312-370`)

Wall/Water 콜라이더를 최소 수의 박스로 병합한다.

```csharp
private void CreateMergedColliders<T>(List<Vector2Int> positions, Transform container, string namePrefix)
```

**알고리즘:**
1. 남은 위치들을 HashSet에 보관
2. row-major 순서로 시작점 선택
3. **가로 확장**: 연속된 x 방향으로 확장
4. **세로 확장**: strip 전체가 존재하는 한 z 방향으로 확장
5. 사각형 영역 제거 후 단일 BoxCollider 생성
6. 반복

**효과:**
- 예: 5×3 Water 영역 → 15개 Collider 대신 1개
- `<T>` 제네릭: `BlockObstacle`(Wall) 또는 `WaterObstacle`(Water) 마커 자동 부착

### Boundary Collider 4면 벽 (`TileMap.cs:412-441`)

```csharp
private void AddBoundaryColliders(Transform container)
{
    float wallH = 4f;   // 높이
    float wallT = 1f;   // 두께

    // 남, 북, 서, 동 4면에 보이지 않는 BoxCollider 벽 배치
    CreateWall(container, ...);  // ×4
}
```

- 각 벽에 `BlockObstacle` 컴포넌트 추가 (`IsBoundary = true`)
- 프로젝타일도 차단, WallPass로도 통과 불가

---

## 11. 렌더링 최적화

### Static Batching (`TileMap.cs:291-292`)

```csharp
container.gameObject.isStatic = true;
StaticBatchingUtility.Combine(container.gameObject);
```

- 모든 Floor Cube, Block, Background Block을 하나의 Static Batch로 통합
- 각 오브젝트에 `isStatic = true` 설정 후 `Combine()` 호출
- Draw Call 대폭 감소 (수백 개 → 머티리얼 수 기준 수 개)

### Background Block Mesh 복사 (`TileMap.cs:398-401`)

```csharp
MeshFilter mf = block.GetComponent<MeshFilter>();
if (mf != null && mf.sharedMesh != null)
    mf.sharedMesh = Instantiate(mf.sharedMesh);
```

- Static Batching은 mesh가 Read/Write 가능해야 함
- 프리팹의 원본 mesh는 Read/Write 비활성화일 수 있으므로 `Instantiate`로 복사본 생성
- 복사본은 자동으로 Read/Write 활성화 상태

### SRP Batcher 호환

- URP Lit 셰이더 사용 시 SRP Batcher가 자동 적용
- Static 오브젝트는 Static Batching이 우선
- 프로젝타일 등 동적 오브젝트는 SRP Batcher가 최선의 최적화

### 최적화 요약표

| 대상 | 기법 | 효과 |
|---|---|---|
| Floor Cube | Static Batching | Draw Call 통합 |
| Block 프리팹 | Static Batching | Draw Call 통합 |
| Background Block | Mesh 복사 + Static Batching | R/W 호환 보장 |
| 바닥 물리 | 단일 BoxCollider | Collider 수 최소화 |
| Wall/Water 물리 | Greedy Merge | Collider 수 최소화 |
| 동적 오브젝트 | SRP Batcher | SetPass 최소화 |

---

## 12. 에디터 도구 (`TileMapEditor.cs`)

### 브러시 모드

```csharp
private enum EBrushMode { None, TilePaint, BlockPlace, SpikePlace, MonsterPlace, SpawnPoint, ExitPoint }
```

| 모드 | 기능 | 드래그 지원 |
|---|---|---|
| TilePaint | Path/Water/Wall 타일 페인팅 | O |
| BlockPlace | 블록 프리팹 배치/제거 | O |
| SpikePlace | Spike 타일 토글 | O |
| MonsterPlace | 몬스터 프리팹 배치/제거 | O |
| SpawnPoint | 플레이어 스폰 위치 설정 | X (클릭) |
| ExitPoint | 출구 위치 설정 | X (클릭) |

### Inspector GUI

- "Generate Preview" / "Clear Preview" 버튼
- 브러시 모드 Enum Popup
- 모드별 옵션:
  - TilePaint: 타일 타입 Popup
  - BlockPlace: Remove 토글 + 프리팹 Popup
  - SpikePlace: Remove 토글
  - MonsterPlace: Remove 토글 + 프리팹 Popup

### Scene View 인터랙션 (`OnSceneGUI`)

```csharp
// Y=0 평면에 레이캐스트
Plane plane = new Plane(Vector3.up, Vector3.zero);
plane.Raycast(ray, out float distance);
Vector3 hitPoint = ray.GetPoint(distance);
Vector2Int grid = tileMap.WorldToGrid(hitPoint);

// 셀 하이라이트
Handles.DrawWireCube(cellCenter, new Vector3(1f, 0.2f, 1f));

// MouseDown/Drag로 페인팅
if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
{
    Undo.RecordObject(tileMap, "...");
    // 타일/블록/몬스터 수정
    EditorUtility.SetDirty(tileMap);
    tileMap.GenerateVisuals();  // 즉시 프리뷰 갱신
}
```

### Gizmo 시각화 (`TileMap.cs:548-563`)

```csharp
private void OnDrawGizmos()
{
    // 스폰 포인트: 노란 구
    Gizmos.color = Color.yellow;
    Gizmos.DrawSphere(GetSpawnWorldPosition() + Vector3.up * 0.5f, 0.3f);

    // 출구: 마젠타 구
    Gizmos.color = Color.magenta;
    Gizmos.DrawSphere(exitPos + Vector3.up * 0.5f, 0.3f);

    // 몬스터 위치: 빨간 구
    Gizmos.color = Color.red;
    foreach (var entry in _placedMonsters)
        Gizmos.DrawSphere(..., 0.25f);
}
```

---

## 부록: TilePassability 컴포넌트 (`TilePassability.cs`)

엔티티(플레이어, 몬스터)에 부착하여 통행 가능 조건을 정의한다.

```csharp
public class TilePassability : MonoBehaviour
{
    [SerializeField] private ETilePassFlag _passFlags = ETilePassFlag.Walk;

    public ETilePassFlag PassFlags { get; set; }

    public void AddFlag(ETilePassFlag flag)   => _passFlags |= flag;
    public void RemoveFlag(ETilePassFlag flag) => _passFlags &= ~flag;
}
```

- 기본값: `Walk` (일반 이동)
- 업그레이드 시 `AddFlag()`로 능력 추가:
  - WallPass → 장애물 통과
  - WaterWalk → 물 위 이동
  - Fly → 모든 비-Wall 타일 통과
