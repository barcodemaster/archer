using System.Collections.Generic;
using UnityEngine;
using static Define;

[System.Serializable]
public class MapGenerationSettings
{
	public int waterRegionCount = 2;
	public int waterMinSize = 2;
	public int waterMaxSize = 4;
	[Range(0f, 0.3f)] public float blockDensity = 0.1f;
	public int seed = 0; // 0 = 매번 랜덤
}

public class TileMap : MonoBehaviour
{
	[SerializeField] private MapData _mapData;
	[SerializeField] private Vector3 _origin;

	// [0] 체커보드 짝수, [1] 홀수, ... 순환 배치
	[Header("Tile Materials")]
	[SerializeField] private Material[] _pathMaterials;
	[SerializeField] private Material _wallMaterial;
	// [0] North Edge  [1] South Edge  [2] West Edge  [3] East Edge  [4] Center/Isolated
	[SerializeField] private Material[] _waterMaterials;

	[Header("Block Prefabs")]
	[SerializeField] private GameObject[] _blockPrefabs;
	[SerializeField] private float _blockYOffset = 0.5f;

	[Header("Monster Prefabs")]
	[SerializeField] private GameObject[] _monsterPrefabs;
	public GameObject[] MonsterPrefabs => _monsterPrefabs;

	[Header("Exit Door")]
	[SerializeField] private GameObject _exitDoorPrefab;

	[Header("Settings")]
	[SerializeField] private float _floorThickness = 0.1f;

	public MapData MapData => _mapData;
	public GameObject[] BlockPrefabs => _blockPrefabs;

	private GameObject _spawnedExitDoor;
	public GameObject SpawnedExitDoor => _spawnedExitDoor;

	private const string VISUALS_CONTAINER = "_TileVisuals";

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
	/// 해당 월드 좌표의 타일을 주어진 플래그로 통과 가능한지 판정한다.
	/// 맵 경계 밖은 무조건 이동 불가.
	/// </summary>
	public bool CanPass(Vector3 worldPos, ETilePassFlag flags)
	{
		if (_mapData == null)
			return true;

		Vector2Int grid = WorldToGrid(worldPos);

		if (grid.x < 0 || grid.x >= _mapData.width || grid.y < 0 || grid.y >= _mapData.height)
			return false;

		ETileType tileType = _mapData.GetTile(grid.x, grid.y);
		return IsTilePassable(tileType, flags);
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
				return (flags & (ETilePassFlag.Fly | ETilePassFlag.WallPass)) != 0;
			case ETileType.Water:
				return (flags & (ETilePassFlag.Fly | ETilePassFlag.WaterWalk)) != 0;
			default:
				return false;
		}
	}

	/// <summary>
	/// 스폰 포인트의 월드 좌표를 반환한다.
	/// </summary>
	public Vector3 GetSpawnWorldPosition()
	{
		if (_mapData == null)
			return _origin;
		return GridToWorld(_mapData.playerSpawnPoint.x, _mapData.playerSpawnPoint.y);
	}

	/// <summary>
	/// 맵 타일과 블록의 3D 시각 오브젝트를 생성한다.
	/// </summary>
	public void GenerateVisuals()
	{
		ClearVisuals();
		_spawnedExitDoor = null;

		if (_mapData == null) return;
		_mapData.InitIfNeeded();

		Transform container = new GameObject(VISUALS_CONTAINER).transform;
		container.SetParent(transform);
		container.localPosition = Vector3.zero;

		for (int z = 0; z < _mapData.height; z++)
		{
			for (int x = 0; x < _mapData.width; x++)
			{
				ETileType tileType = _mapData.GetTile(x, z);
				Vector3 tileCenter = GridToWorld(x, z);

				// Floor cube: top surface at y=0
				GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
				floor.name = $"Floor_{x}_{z}";
				floor.transform.SetParent(container);
				floor.transform.position = tileCenter + Vector3.down * (_floorThickness / 2f);
				floor.transform.localScale = new Vector3(1f, _floorThickness, 1f);

				Material mat = GetMaterialForType(tileType, x, z);
				if (mat != null)
					floor.GetComponent<Renderer>().material = mat;

				// Water 타일에 물리 차단용 콜라이더 추가
				if (tileType == ETileType.Water)
				{
					GameObject waterWall = new GameObject("WaterWall");
					waterWall.transform.SetParent(container);
					waterWall.transform.position = tileCenter + Vector3.up * 1f;
					BoxCollider bc = waterWall.AddComponent<BoxCollider>();
					bc.size = new Vector3(1f, 2f, 1f);
					bc.isTrigger = false;
				}

				// Block prefab
				int blockIdx = _mapData.GetBlockIndex(x, z);
				if (blockIdx >= 0 && _blockPrefabs != null && blockIdx < _blockPrefabs.Length && _blockPrefabs[blockIdx] != null)
				{
					GameObject block = Instantiate(_blockPrefabs[blockIdx], container);
					block.name = $"Block_{x}_{z}";
					block.transform.position = tileCenter + Vector3.up * _blockYOffset;
				}
			}
		}

		// ExitDoor 배치
		if (_mapData.exitPoint.x >= 0 && _exitDoorPrefab != null)
		{
			Vector3 exitPos = GridToWorld(_mapData.exitPoint.x, _mapData.exitPoint.y);
			_spawnedExitDoor = Instantiate(_exitDoorPrefab, container);
			_spawnedExitDoor.name = "ExitDoor";
			_spawnedExitDoor.transform.position = exitPos;
			if (_spawnedExitDoor.GetComponent<ExitDoor>() == null)
				_spawnedExitDoor.AddComponent<ExitDoor>();
			_spawnedExitDoor.SetActive(false);
		}

		AddBoundaryColliders(container);
	}

	/// <summary>
	/// 맵 4면 경계에 물리 차단 벽을 추가한다.
	/// </summary>
	private void AddBoundaryColliders(Transform container)
	{
		int w = _mapData.width;
		int h = _mapData.height;
		float wallH = 4f;
		float wallT = 1f;
		Vector3 orig = _origin;

		float cx = w / 2f - 0.5f;
		float cz = h / 2f - 0.5f;

		// South: z=-0.5 바깥
		CreateWall(container, orig + new Vector3(cx, wallH / 2f, -0.5f - wallT / 2f),
				   new Vector3(w + wallT * 2, wallH, wallT));
		// North: z=h-0.5 바깥
		CreateWall(container, orig + new Vector3(cx, wallH / 2f, h - 0.5f + wallT / 2f),
				   new Vector3(w + wallT * 2, wallH, wallT));
		// West: x=-0.5 바깥
		CreateWall(container, orig + new Vector3(-0.5f - wallT / 2f, wallH / 2f, cz),
				   new Vector3(wallT, wallH, h + wallT * 2));
		// East: x=w-0.5 바깥
		CreateWall(container, orig + new Vector3(w - 0.5f + wallT / 2f, wallH / 2f, cz),
				   new Vector3(wallT, wallH, h + wallT * 2));
	}

	private void CreateWall(Transform parent, Vector3 pos, Vector3 size)
	{
		GameObject wall = new GameObject("BoundaryWall");
		wall.transform.SetParent(parent);
		wall.transform.position = pos;
		BoxCollider bc = wall.AddComponent<BoxCollider>();
		bc.size = size;
		bc.isTrigger = false;
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

		bool n = _mapData.GetTile(x, z + 1) == ETileType.Water;
		bool s = _mapData.GetTile(x, z - 1) == ETileType.Water;
		bool w = _mapData.GetTile(x - 1, z) == ETileType.Water;
		bool e = _mapData.GetTile(x + 1, z) == ETileType.Water;
		int count = (n ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0) + (e ? 1 : 0);

		if (count >= 2) return _waterMaterials[4]; // Center
		if (s && !n && !w && !e) return _waterMaterials[0]; // 아래만 연결 → North Edge
		if (n && !s && !w && !e) return _waterMaterials[1]; // 위만 연결 → South Edge
		if (e && !n && !s && !w) return _waterMaterials[2]; // 오른쪽만 연결 → West Edge
		if (w && !n && !s && !e) return _waterMaterials[3]; // 왼쪽만 연결 → East Edge
		return _waterMaterials[4]; // 고립 → Center
	}

	/// <summary>
	/// 규칙 기반 랜덤 맵을 생성한다. 스폰→Exit BFS 경로를 보장한다.
	/// </summary>
	public void GenerateRandomMap(MapGenerationSettings settings)
	{
		if (_mapData == null) return;
		_mapData.InitIfNeeded();

		System.Random rng = settings.seed != 0 ? new System.Random(settings.seed) : new System.Random();
		int w = _mapData.width;
		int h = _mapData.height;

		// 1. 전체 Path로 초기화
		for (int i = 0; i < _mapData.tiles.Length; i++)
			_mapData.tiles[i] = ETileType.Path;
		for (int i = 0; i < _mapData.blockPrefabIndices.Length; i++)
			_mapData.blockPrefabIndices[i] = -1;

		// 2. Water 영역 배치
		for (int r = 0; r < settings.waterRegionCount; r++)
		{
			int regionW = rng.Next(settings.waterMinSize, settings.waterMaxSize + 1);
			int regionH = rng.Next(settings.waterMinSize, settings.waterMaxSize + 1);
			int startX = rng.Next(2, w - regionW - 2);
			int startZ = rng.Next(2, h - regionH - 2);

			for (int dz = 0; dz < regionH; dz++)
				for (int dx = 0; dx < regionW; dx++)
					_mapData.tiles[(startZ + dz) * w + (startX + dx)] = ETileType.Water;
		}

		// 4. 스폰/Exit 포인트 결정
		Vector2Int spawnPt = FindNearestPath(1, 1, _mapData);
		Vector2Int exitPt = FindNearestPath(w - 2, h - 2, _mapData);
		_mapData.playerSpawnPoint = spawnPt;
		_mapData.exitPoint = exitPt;

		// 5. BFS 경로 보장 — 막혀있으면 직선 강제 개통
		if (!HasWalkPath(spawnPt, exitPt, _mapData))
		{
			int cx = spawnPt.x, cz = spawnPt.y;
			while (cx != exitPt.x || cz != exitPt.y)
			{
				_mapData.tiles[cz * w + cx] = ETileType.Path;
				if (cx < exitPt.x) cx++;
				else if (cx > exitPt.x) cx--;
				else if (cz < exitPt.y) cz++;
				else cz--;
			}
		}

		// 6. 블록 배치 (BFS 재확인하며 배치)
		if (_blockPrefabs != null && _blockPrefabs.Length > 0 && settings.blockDensity > 0f)
		{
			int blocksToPlace = Mathf.RoundToInt(w * h * settings.blockDensity);

			List<Vector2Int> candidates = new List<Vector2Int>();
			for (int z = 1; z < h - 1; z++)
				for (int x = 1; x < w - 1; x++)
				{
					if (_mapData.GetTile(x, z) != ETileType.Path) continue;
					if (IsNearPoint(x, z, spawnPt, 2) || IsNearPoint(x, z, exitPt, 2)) continue;
					candidates.Add(new Vector2Int(x, z));
				}

			// Fisher-Yates 셔플
			for (int i = candidates.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(candidates[i], candidates[j]) = (candidates[j], candidates[i]);
			}

			int placed = 0;
			foreach (Vector2Int c in candidates)
			{
				if (placed >= blocksToPlace) break;

				int blockIdx = rng.Next(_blockPrefabs.Length);
				_mapData.blockPrefabIndices[c.y * w + c.x] = blockIdx;

				if (!HasWalkPath(spawnPt, exitPt, _mapData))
					_mapData.blockPrefabIndices[c.y * w + c.x] = -1; // 경로 차단 시 취소
				else
					placed++;
			}
		}
	}

	/// <summary>
	/// 지정 좌표에서 가장 가까운 Path 타일 좌표를 BFS로 찾는다.
	/// </summary>
	private Vector2Int FindNearestPath(int startX, int startZ, MapData mapData)
	{
		if (mapData.GetTile(startX, startZ) == ETileType.Path)
			return new Vector2Int(startX, startZ);

		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
		queue.Enqueue(new Vector2Int(startX, startZ));
		visited.Add(new Vector2Int(startX, startZ));

		int[] dx = { 0, 0, 1, -1 };
		int[] dz = { 1, -1, 0, 0 };

		while (queue.Count > 0)
		{
			Vector2Int cur = queue.Dequeue();
			if (mapData.GetTile(cur.x, cur.y) == ETileType.Path)
				return cur;

			for (int i = 0; i < 4; i++)
			{
				Vector2Int next = new Vector2Int(cur.x + dx[i], cur.y + dz[i]);
				if (!visited.Contains(next) && next.x >= 0 && next.x < mapData.width && next.y >= 0 && next.y < mapData.height)
				{
					visited.Add(next);
					queue.Enqueue(next);
				}
			}
		}

		return new Vector2Int(startX, startZ);
	}

	/// <summary>
	/// BFS로 Walk 통행 가능 경로가 from→to 사이에 존재하는지 확인한다.
	/// 블록이 배치된 Path 타일은 통과 불가로 처리한다.
	/// </summary>
	private bool HasWalkPath(Vector2Int from, Vector2Int to, MapData mapData)
	{
		if (from == to) return true;

		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
		queue.Enqueue(from);
		visited.Add(from);

		int[] dx = { 0, 0, 1, -1 };
		int[] dz = { 1, -1, 0, 0 };

		while (queue.Count > 0)
		{
			Vector2Int cur = queue.Dequeue();
			if (cur == to) return true;

			for (int i = 0; i < 4; i++)
			{
				Vector2Int next = new Vector2Int(cur.x + dx[i], cur.y + dz[i]);
				if (visited.Contains(next)) continue;
				if (next.x < 0 || next.x >= mapData.width || next.y < 0 || next.y >= mapData.height) continue;
				if (mapData.GetTile(next.x, next.y) != ETileType.Path) continue;
				if (mapData.GetBlockIndex(next.x, next.y) >= 0) continue;
				visited.Add(next);
				queue.Enqueue(next);
			}
		}

		return false;
	}

	private bool IsNearPoint(int x, int z, Vector2Int point, int radius)
	{
		return Mathf.Abs(x - point.x) <= radius && Mathf.Abs(z - point.y) <= radius;
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (_mapData == null) return;

		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(GetSpawnWorldPosition() + Vector3.up * 0.5f, 0.3f);

		if (_mapData.exitPoint.x >= 0)
		{
			Gizmos.color = Color.magenta;
			Vector3 exitPos = GridToWorld(_mapData.exitPoint.x, _mapData.exitPoint.y);
			Gizmos.DrawSphere(exitPos + Vector3.up * 0.5f, 0.3f);
		}

		if (_mapData.monsterPrefabIndices != null)
		{
			Gizmos.color = Color.red;
			for (int z = 0; z < _mapData.height; z++)
				for (int x = 0; x < _mapData.width; x++)
					if (_mapData.GetMonsterIndex(x, z) >= 0)
						Gizmos.DrawSphere(GridToWorld(x, z) + Vector3.up * 0.5f, 0.25f);
		}
	}
#endif
}
