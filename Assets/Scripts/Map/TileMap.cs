using System.Collections.Generic;
using UnityEngine;
using static Define;

[System.Serializable]
public struct TileEntry
{
	public Vector2Int pos;
	public ETileType type;
}

[System.Serializable]
public struct BlockEntry
{
	public Vector2Int pos;
	public int prefabIndex;
}

[System.Serializable]
public struct MonsterEntry
{
	public Vector2Int pos;
	public int prefabIndex;
}

public class TileMap : MonoBehaviour
{
	[SerializeField] private Vector3 _origin;

	[Header("Map Size")]
	[SerializeField] private int _width = 20;
	[SerializeField] private int _height = 20;

	[Header("Map Data")]
	[SerializeField] private List<TileEntry> _tileOverrides = new List<TileEntry>();
	[SerializeField] private List<BlockEntry> _placedBlocks = new List<BlockEntry>();
	[SerializeField] private List<MonsterEntry> _placedMonsters = new List<MonsterEntry>();
	[SerializeField] private List<BlockEntry> _placedTraps = new List<BlockEntry>();
	[SerializeField] private Vector2Int _playerSpawnPoint = new Vector2Int(1, 1);
	[SerializeField] private Vector2Int _exitPoint = new Vector2Int(-1, -1);

	// [0] 체커보드 짝수, [1] 홀수, ... 순환 배치
	[Header("Tile Materials")]
	[SerializeField] private Material[] _pathMaterials;
	[SerializeField] private Material _wallMaterial;
	// [0] North Edge  [1] South Edge  [2] West Edge  [3] East Edge  [4] Center/Isolated
	[SerializeField] private Material[] _waterMaterials;
	[Header("Block Prefabs")]
	[SerializeField] private GameObject[] _blockPrefabs;
	[SerializeField] private GameObject _spikePrefab;
	[SerializeField] private float _blockYOffset = 0.5f;

	[Header("Trap Prefabs")]
	[SerializeField] private GameObject[] _trapPrefabs;
	[SerializeField] private float _trapYOffset = 0.5f;
	public GameObject[] TrapPrefabs => _trapPrefabs;

	[Header("Monster Prefabs")]
	[SerializeField] private GameObject[] _monsterPrefabs;
	public GameObject[] MonsterPrefabs => _monsterPrefabs;

	[Header("Exit Door")]
	[SerializeField] private GameObject _exitDoorPrefab;

	[Header("Background")]
	[SerializeField] private GameObject _backgroundBlockPrefab;
	[SerializeField] private int _backgroundExtent = 5;
	[SerializeField] private float _backgroundYOffset = -1.5f;

	[Header("Settings")]
	[SerializeField] private float _floorThickness = 0.1f;

	public int Width => _width;
	public int Height => _height;

	/// <summary>
	/// 맵의 xz 평면 월드 범위를 반환한다.
	/// </summary>
	public void GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ)
	{
		minX = _origin.x - 0.5f;
		maxX = _origin.x + _width - 0.5f;
		minZ = _origin.z - 0.5f;
		maxZ = _origin.z + _height - 0.5f;
	}
	public List<MonsterEntry> PlacedMonsters => _placedMonsters;
	public GameObject[] BlockPrefabs => _blockPrefabs;

	private GameObject _spawnedExitDoor;
	public GameObject SpawnedExitDoor => _spawnedExitDoor;

	private const string VISUALS_CONTAINER = "_TileVisuals";

	// Dictionary 캐시 (런타임 O(1) 조회용)
	private Dictionary<Vector2Int, ETileType> _tileCache;
	private Dictionary<Vector2Int, int> _blockCache;
	private Dictionary<Vector2Int, int> _trapCache;

	/// <summary>
	/// _tileOverrides/_placedBlocks 리스트를 Dictionary로 변환하여 캐싱한다.
	/// </summary>
	private void BuildCache()
	{
		_tileCache = new Dictionary<Vector2Int, ETileType>(_tileOverrides.Count);
		foreach (var e in _tileOverrides)
			_tileCache[e.pos] = e.type;

		_blockCache = new Dictionary<Vector2Int, int>(_placedBlocks.Count);
		foreach (var b in _placedBlocks)
			_blockCache[b.pos] = b.prefabIndex;

		_trapCache = new Dictionary<Vector2Int, int>(_placedTraps.Count);
		foreach (var t in _placedTraps)
			_trapCache[t.pos] = t.prefabIndex;
	}

	/// <summary>
	/// 월드 좌표를 그리드 좌표로 변환한다.
	/// </summary>
	public Vector2Int WorldToGrid(Vector3 worldPos)
	{
		int x = Mathf.FloorToInt(worldPos.x - _origin.x + 0.5f);
		int z = Mathf.FloorToInt(worldPos.z - _origin.z + 0.5f);
		return new Vector2Int(x, z);
	}

	/// <summary>
	/// 그리드 좌표를 타일 중심 월드 좌표로 변환한다.
	/// </summary>
	public Vector3 GridToWorld(int x, int z)
	{
		return new Vector3(_origin.x + x, _origin.y, _origin.z + z);
	}

	/// <summary>
	/// 지정 좌표의 타일 타입을 반환한다. 범위 밖이면 Wall, 기본값은 Path.
	/// </summary>
	public ETileType GetTile(int x, int z)
	{
		if (x < 0 || x >= _width || z < 0 || z >= _height) return ETileType.Wall;

		// 캐시가 있으면 O(1) 조회
		if (_tileCache != null)
		{
			if (_tileCache.TryGetValue(new Vector2Int(x, z), out ETileType type))
				return type;
			return ETileType.Path;
		}

		// 캐시 없음 — 에디터 호환 fallback
		foreach (var e in _tileOverrides)
			if (e.pos.x == x && e.pos.y == z) return e.type;
		return ETileType.Path;
	}

	private int GetBlockIndex(int x, int z)
	{
		// 캐시가 있으면 O(1) 조회
		if (_blockCache != null)
		{
			if (_blockCache.TryGetValue(new Vector2Int(x, z), out int idx))
				return idx;
			return -1;
		}

		// 캐시 없음 — 에디터 호환 fallback
		foreach (var b in _placedBlocks)
			if (b.pos.x == x && b.pos.y == z) return b.prefabIndex;
		return -1;
	}

	private int GetTrapIndex(int x, int z)
	{
		if (_trapCache != null)
		{
			if (_trapCache.TryGetValue(new Vector2Int(x, z), out int idx)) return idx;
			return -1;
		}
		foreach (var t in _placedTraps)
			if (t.pos.x == x && t.pos.y == z) return t.prefabIndex;
		return -1;
	}

	/// <summary>
	/// 해당 그리드 좌표에 블록이 없는지 반환한다. (A* 경로 탐색용)
	/// </summary>
	public bool CanPassBlock(int x, int z) => GetBlockIndex(x, z) < 0;

	/// <summary>
	/// 해당 월드 좌표의 타일을 주어진 플래그로 통과 가능한지 판정한다.
	/// 맵 경계 밖은 무조건 이동 불가.
	/// </summary>
	public bool CanPass(Vector3 worldPos, ETilePassFlag flags)
	{
		if (_width == 0 || _height == 0) return true;

		Vector2Int grid = WorldToGrid(worldPos);
		if (grid.x < 0 || grid.x >= _width || grid.y < 0 || grid.y >= _height) return false;

		ETileType tileType = GetTile(grid.x, grid.y);
		if (!IsTilePassable(tileType, flags)) return false;
		if (GetBlockIndex(grid.x, grid.y) >= 0)
			return (flags & ETilePassFlag.WallPass) != 0;
		return true;
	}

	/// <summary>
	/// 타일 타입과 통과 플래그 조합으로 통과 가능 여부를 반환한다.
	/// </summary>
	public static bool IsTilePassable(ETileType tileType, ETilePassFlag flags)
	{
		switch (tileType)
		{
			case ETileType.Path:
				return (flags & (ETilePassFlag.Walk | ETilePassFlag.Fly)) != 0;
			case ETileType.Wall:
				return (flags & ETilePassFlag.WallPass) != 0;
			case ETileType.Water:
				return (flags & (ETilePassFlag.Fly | ETilePassFlag.WaterWalk)) != 0;
			case ETileType.Spike:
				return (flags & (ETilePassFlag.Walk | ETilePassFlag.Fly)) != 0;
			default:
				return false;
		}
	}

	/// <summary>
	/// 스폰 포인트의 월드 좌표를 반환한다.
	/// </summary>
	public Vector3 GetSpawnWorldPosition()
	{
		return GridToWorld(_playerSpawnPoint.x, _playerSpawnPoint.y);
	}

	/// <summary>
	/// 맵 타일과 블록의 3D 시각 오브젝트를 생성한다.
	/// </summary>
	public void GenerateVisuals()
	{
		ClearVisuals();
		_spawnedExitDoor = null;
		BuildCache();

		Transform container = new GameObject(VISUALS_CONTAINER).transform;
		container.SetParent(transform);
		container.localPosition = Vector3.zero;

		List<Vector2Int> wallPositions = new List<Vector2Int>();
		List<Vector2Int> waterPositions = new List<Vector2Int>();
		List<Vector2Int> blockPositions = new List<Vector2Int>();
		List<Vector2Int> spikePositions = new List<Vector2Int>();

		for (int z = 0; z < _height; z++)
		{
			for (int x = 0; x < _width; x++)
			{
				ETileType tileType = GetTile(x, z);
				Vector3 tileCenter = GridToWorld(x, z);

				// Floor quad: y=0 평면에 배치, 2.5D 스타일
				GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
				floor.name = $"Floor_{x}_{z}";
				floor.transform.SetParent(container);
				floor.transform.position = tileCenter;
				floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				floor.transform.localScale = Vector3.one;
				floor.isStatic = true;

				// 개별 MeshCollider 제거 (단일 FloorCollider로 대체)
				Object.DestroyImmediate(floor.GetComponent<MeshCollider>());

				Material mat = GetMaterialForType(tileType, x, z);
				if (mat != null)
				{
					floor.GetComponent<Renderer>().sharedMaterial = mat;
				}

				// Spike 타일에 프리팹 배치 (비주얼만 유지, 콜라이더는 병합)
				if (tileType == ETileType.Spike && _spikePrefab != null)
				{
					GameObject spike = Instantiate(_spikePrefab, container);
					spike.name = $"Spike_{x}_{z}";
					spike.transform.position = tileCenter + Vector3.up * _blockYOffset;
					foreach (var col in spike.GetComponentsInChildren<Collider>())
						Object.DestroyImmediate(col);
					var st = spike.GetComponent<SpikeTile>();
					if (st != null) Object.DestroyImmediate(st);
					spikePositions.Add(new Vector2Int(x, z));
				}

				// Wall/Water 위치 수집 (개별 콜라이더 대신 병합)
				if (tileType == ETileType.Water)
					waterPositions.Add(new Vector2Int(x, z));
				else if (tileType == ETileType.Wall)
					wallPositions.Add(new Vector2Int(x, z));

				// Block prefab (비주얼만 유지, 콜라이더는 병합)
				int blockIdx = GetBlockIndex(x, z);
				if (blockIdx >= 0 && _blockPrefabs != null && blockIdx < _blockPrefabs.Length && _blockPrefabs[blockIdx] != null)
				{
					GameObject block = Instantiate(_blockPrefabs[blockIdx], container);
					block.name = $"Block_{x}_{z}";
					block.transform.position = tileCenter + Vector3.up * _blockYOffset;
					foreach (var col in block.GetComponentsInChildren<Collider>())
						Object.DestroyImmediate(col);
					blockPositions.Add(new Vector2Int(x, z));
				}

				// Trap 프리팹 (콜라이더 유지, StaticBatching 제외, 애니메이션 유지)
				int trapIdx = GetTrapIndex(x, z);
				if (trapIdx >= 0 && _trapPrefabs != null && trapIdx < _trapPrefabs.Length && _trapPrefabs[trapIdx] != null)
				{
					GameObject trap = Instantiate(_trapPrefabs[trapIdx], transform);
					trap.name = $"Trap_{x}_{z}";
					trap.transform.position = tileCenter + Vector3.up * _trapYOffset;
				}
			}
		}

		// 단일 FloorCollider 생성 (맵 전체 커버)
		CreateFloorCollider(container);

		// Wall/Water/Block/Spike Collider 병합
		CreateMergedColliders<BlockObstacle>(wallPositions, container, "WallCollider");
		CreateMergedColliders<WaterObstacle>(waterPositions, container, "WaterCollider");
		CreateMergedColliders<BlockObstacle>(blockPositions, container, "BlockCollider");
		CreateMergedTriggerColliders<SpikeTile>(spikePositions, container, "SpikeCollider");

		// ExitDoor 배치
		if (_exitPoint.x >= 0 && _exitDoorPrefab != null)
		{
			Vector3 exitPos = GridToWorld(_exitPoint.x, _exitPoint.y);
			_spawnedExitDoor = Instantiate(_exitDoorPrefab, container);
			_spawnedExitDoor.name = "ExitDoor";
			_spawnedExitDoor.transform.position = exitPos;
			if (_spawnedExitDoor.GetComponent<ExitDoor>() == null)
				_spawnedExitDoor.AddComponent<ExitDoor>();
		}

		AddBoundaryColliders(container);
		GenerateBackgroundBlocks(container);

		// Static Batching
		container.gameObject.isStatic = true;
		StaticBatchingUtility.Combine(container.gameObject);

		// 양옆 장식물 배치
		MapDecorationController deco = GetComponent<MapDecorationController>();
		if (deco != null)
			deco.GenerateDecorations(this);

		// 가장자리 안개 강화
		EdgeFogController fog = GetComponent<EdgeFogController>();
		if (fog != null)
			fog.GenerateFog(this);
	}

	/// <summary>
	/// 맵 전체를 덮는 단일 바닥 콜라이더를 생성한다.
	/// </summary>
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

	/// <summary>
	/// Greedy rectangle merge로 인접 타일을 병합하여 최소 수의 BoxCollider를 생성한다.
	/// </summary>
	private void CreateMergedColliders<T>(List<Vector2Int> positions, Transform container, string namePrefix) where T : Component
	{
		if (positions.Count == 0) return;

		HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(positions);

		while (remaining.Count > 0)
		{
			// row-major 순서로 시작점 찾기
			Vector2Int start = default;
			int minVal = int.MaxValue;
			foreach (var p in remaining)
			{
				int val = p.y * _width + p.x;
				if (val < minVal) { minVal = val; start = p; }
			}

			// 가로(x)로 확장
			int xEnd = start.x;
			while (remaining.Contains(new Vector2Int(xEnd + 1, start.y)))
				xEnd++;

			// 세로(z)로 확장 — strip 전체가 존재해야 함
			int zEnd = start.y;
			bool canExpand = true;
			while (canExpand)
			{
				int nextZ = zEnd + 1;
				for (int xi = start.x; xi <= xEnd; xi++)
				{
					if (!remaining.Contains(new Vector2Int(xi, nextZ)))
					{
						canExpand = false;
						break;
					}
				}
				if (canExpand) zEnd = nextZ;
			}

			// remaining에서 사각형 영역 제거
			for (int zi = start.y; zi <= zEnd; zi++)
				for (int xi = start.x; xi <= xEnd; xi++)
					remaining.Remove(new Vector2Int(xi, zi));

			// 병합된 사각형에 대한 단일 BoxCollider 생성
			int sizeX = xEnd - start.x + 1;
			int sizeZ = zEnd - start.y + 1;
			float centerX = _origin.x + start.x + (sizeX - 1) / 2f;
			float centerZ = _origin.z + start.y + (sizeZ - 1) / 2f;

			GameObject obj = new GameObject($"{namePrefix}_{start.x}_{start.y}");
			obj.transform.SetParent(container);
			obj.transform.position = new Vector3(centerX, _origin.y + 1f, centerZ);
			BoxCollider bc = obj.AddComponent<BoxCollider>();
			bc.size = new Vector3(sizeX, 2f, sizeZ);
			bc.isTrigger = false;
			obj.AddComponent<T>();
		}
	}

	/// <summary>
	/// Greedy rectangle merge로 인접 타일을 병합하여 isTrigger BoxCollider를 생성한다.
	/// </summary>
	private void CreateMergedTriggerColliders<T>(List<Vector2Int> positions, Transform container, string namePrefix) where T : Component
	{
		if (positions.Count == 0) return;

		HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(positions);

		while (remaining.Count > 0)
		{
			Vector2Int start = default;
			int minVal = int.MaxValue;
			foreach (var p in remaining)
			{
				int val = p.y * _width + p.x;
				if (val < minVal) { minVal = val; start = p; }
			}

			int xEnd = start.x;
			while (remaining.Contains(new Vector2Int(xEnd + 1, start.y)))
				xEnd++;

			int zEnd = start.y;
			bool canExpand = true;
			while (canExpand)
			{
				int nextZ = zEnd + 1;
				for (int xi = start.x; xi <= xEnd; xi++)
				{
					if (!remaining.Contains(new Vector2Int(xi, nextZ)))
					{
						canExpand = false;
						break;
					}
				}
				if (canExpand) zEnd = nextZ;
			}

			for (int zi = start.y; zi <= zEnd; zi++)
				for (int xi = start.x; xi <= xEnd; xi++)
					remaining.Remove(new Vector2Int(xi, zi));

			int sizeX = xEnd - start.x + 1;
			int sizeZ = zEnd - start.y + 1;
			float centerX = _origin.x + start.x + (sizeX - 1) / 2f;
			float centerZ = _origin.z + start.y + (sizeZ - 1) / 2f;

			GameObject obj = new GameObject($"{namePrefix}_{start.x}_{start.y}");
			obj.transform.SetParent(container);
			obj.transform.position = new Vector3(centerX, _origin.y + 1f, centerZ);
			BoxCollider bc = obj.AddComponent<BoxCollider>();
			bc.size = new Vector3(sizeX, 2f, sizeZ);
			bc.isTrigger = true;
			obj.AddComponent<T>();
		}
	}

	/// <summary>
	/// 맵 바깥에 낮은 블록 프리팹을 깔아 배경을 생성한다.
	/// </summary>
	private void GenerateBackgroundBlocks(Transform container)
	{
		if (_backgroundBlockPrefab == null) return;

		for (int z = -_backgroundExtent; z < _height + _backgroundExtent; z++)
		{
			for (int x = -_backgroundExtent; x < _width + _backgroundExtent; x++)
			{
				// 맵 내부 좌표는 스킵
				if (x >= 0 && x < _width && z >= 0 && z < _height) continue;

				GameObject block = Instantiate(_backgroundBlockPrefab, container);
				block.name = $"BgBlock_{x}_{z}";
				block.transform.position = GridToWorld(x, z) + Vector3.up * _backgroundYOffset;

				// 콜라이더 제거
				Collider col = block.GetComponentInChildren<Collider>();
				if (col != null)
				{
					if (Application.isPlaying) Destroy(col);
					else DestroyImmediate(col);
				}

				// mesh를 Read/Write 가능한 복사본으로 교체하여 Static Batching 허용
				MeshFilter mf = block.GetComponent<MeshFilter>();
				if (mf != null && mf.sharedMesh != null)
					mf.sharedMesh = Instantiate(mf.sharedMesh);

				// Static Batching 활용
				block.gameObject.isStatic = true;
			}
		}
	}

	/// <summary>
	/// 맵 4면 경계에 물리 차단 벽을 추가한다.
	/// </summary>
	private void AddBoundaryColliders(Transform container)
	{
		float wallH = 4f;
		float wallT = 1f;
		Vector3 orig = _origin;

		float cx = _width / 2f - 0.5f;
		float cz = _height / 2f - 0.5f;

		CreateWall(container, orig + new Vector3(cx, wallH / 2f, -0.5f - wallT / 2f),
				   new Vector3(_width + wallT * 2, wallH, wallT));
		CreateWall(container, orig + new Vector3(cx, wallH / 2f, _height - 0.5f + wallT / 2f),
				   new Vector3(_width + wallT * 2, wallH, wallT));
		CreateWall(container, orig + new Vector3(-0.5f - wallT / 2f, wallH / 2f, cz),
				   new Vector3(wallT, wallH, _height + wallT * 2));
		CreateWall(container, orig + new Vector3(_width - 0.5f + wallT / 2f, wallH / 2f, cz),
				   new Vector3(wallT, wallH, _height + wallT * 2));
	}

	private void CreateWall(Transform parent, Vector3 pos, Vector3 size)
	{
		GameObject wall = new GameObject("BoundaryWall");
		wall.transform.SetParent(parent);
		wall.transform.position = pos;
		BoxCollider bc = wall.AddComponent<BoxCollider>();
		bc.size = size;
		bc.isTrigger = false;
		BlockObstacle obstacle = wall.AddComponent<BlockObstacle>();
		obstacle.IsBoundary = true;
	}

	/// <summary>
	/// 생성된 시각 오브젝트를 모두 제거한다.
	/// </summary>
	public void ClearVisuals()
	{
		Transform container = transform.Find(VISUALS_CONTAINER);
		if (container != null)
		{
			if (Application.isPlaying)
				Destroy(container.gameObject);
			else
				DestroyImmediate(container.gameObject);
		}

		// container 외부에 생성된 Trap 오브젝트 정리
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			var child = transform.GetChild(i);
			if (child.name.StartsWith("Trap_"))
			{
				if (Application.isPlaying) Destroy(child.gameObject);
				else DestroyImmediate(child.gameObject);
			}
		}
	}

	/// <summary>
	/// 타일 타입과 좌표에 해당하는 머티리얼을 반환한다.
	/// </summary>
	private Material GetMaterialForType(ETileType tileType, int x, int z)
	{
		switch (tileType)
		{
			case ETileType.Path:
				if (_pathMaterials != null && _pathMaterials.Length > 0)
					return _pathMaterials[(x + z) % _pathMaterials.Length];
				return null;
			case ETileType.Wall:
				return _wallMaterial;
			case ETileType.Water:
				return GetWaterMaterial(x, z);
			case ETileType.Spike:
				if (_pathMaterials != null && _pathMaterials.Length > 0)
					return _pathMaterials[(x + z) % _pathMaterials.Length];
				return null;
			default:
				return null;
		}
	}

	/// <summary>
	/// 인접 Water 타일 패턴에 따라 Water 머티리얼을 선택한다.
	/// 배열 인덱스: [0] North Edge, [1] South Edge, [2] West Edge, [3] East Edge, [4] Center/Isolated
	/// </summary>
	private Material GetWaterMaterial(int x, int z)
	{
		if (_waterMaterials == null || _waterMaterials.Length < 5)
			return (_waterMaterials != null && _waterMaterials.Length > 0) ? _waterMaterials[0] : null;

		bool n = GetTile(x, z + 1) == ETileType.Water;
		bool s = GetTile(x, z - 1) == ETileType.Water;
		bool w = GetTile(x - 1, z) == ETileType.Water;
		bool e = GetTile(x + 1, z) == ETileType.Water;
		int count = (n ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0) + (e ? 1 : 0);

		if (count >= 2) return _waterMaterials[4];
		if (s && !n && !w && !e) return _waterMaterials[0];
		if (n && !s && !w && !e) return _waterMaterials[1];
		if (e && !n && !s && !w) return _waterMaterials[2];
		if (w && !n && !s && !e) return _waterMaterials[3];
		return _waterMaterials[4];
	}

#if UNITY_EDITOR
	public void SetTile(int x, int z, ETileType type)
	{
		for (int i = 0; i < _tileOverrides.Count; i++)
			if (_tileOverrides[i].pos.x == x && _tileOverrides[i].pos.y == z)
			{
				if (type == ETileType.Path) _tileOverrides.RemoveAt(i);
				else _tileOverrides[i] = new TileEntry { pos = new Vector2Int(x, z), type = type };
				return;
			}
		if (type != ETileType.Path)
			_tileOverrides.Add(new TileEntry { pos = new Vector2Int(x, z), type = type });
	}

	public void SetBlock(int x, int z, int prefabIndex)
	{
		for (int i = 0; i < _placedBlocks.Count; i++)
			if (_placedBlocks[i].pos.x == x && _placedBlocks[i].pos.y == z)
			{
				if (prefabIndex < 0) _placedBlocks.RemoveAt(i);
				else _placedBlocks[i] = new BlockEntry { pos = new Vector2Int(x, z), prefabIndex = prefabIndex };
				return;
			}
		if (prefabIndex >= 0)
			_placedBlocks.Add(new BlockEntry { pos = new Vector2Int(x, z), prefabIndex = prefabIndex });
	}

	public void SetMonster(int x, int z, int prefabIndex)
	{
		for (int i = 0; i < _placedMonsters.Count; i++)
			if (_placedMonsters[i].pos.x == x && _placedMonsters[i].pos.y == z)
			{
				if (prefabIndex < 0) _placedMonsters.RemoveAt(i);
				else _placedMonsters[i] = new MonsterEntry { pos = new Vector2Int(x, z), prefabIndex = prefabIndex };
				return;
			}
		if (prefabIndex >= 0)
			_placedMonsters.Add(new MonsterEntry { pos = new Vector2Int(x, z), prefabIndex = prefabIndex });
	}

	public void SetTrap(int x, int z, int prefabIndex)
	{
		for (int i = 0; i < _placedTraps.Count; i++)
			if (_placedTraps[i].pos.x == x && _placedTraps[i].pos.y == z)
			{
				if (prefabIndex < 0) _placedTraps.RemoveAt(i);
				else _placedTraps[i] = new BlockEntry { pos = new Vector2Int(x, z), prefabIndex = prefabIndex };
				return;
			}
		if (prefabIndex >= 0)
			_placedTraps.Add(new BlockEntry { pos = new Vector2Int(x, z), prefabIndex = prefabIndex });
	}

	public void SetSpawnPoint(Vector2Int p) => _playerSpawnPoint = p;
	public void SetExitPoint(Vector2Int p) => _exitPoint = p;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(GetSpawnWorldPosition() + Vector3.up * 0.5f, 0.3f);

		if (_exitPoint.x >= 0)
		{
			Gizmos.color = Color.magenta;
			Vector3 exitPos = GridToWorld(_exitPoint.x, _exitPoint.y);
			Gizmos.DrawSphere(exitPos + Vector3.up * 0.5f, 0.3f);
		}

		Gizmos.color = Color.red;
		foreach (var entry in _placedMonsters)
			Gizmos.DrawSphere(GridToWorld(entry.pos.x, entry.pos.y) + Vector3.up * 0.5f, 0.25f);
	}
#endif
}
