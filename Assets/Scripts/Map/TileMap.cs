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

	[Header("Monster Prefabs")]
	[SerializeField] private GameObject[] _monsterPrefabs;
	public GameObject[] MonsterPrefabs => _monsterPrefabs;

	[Header("Exit Door")]
	[SerializeField] private GameObject _exitDoorPrefab;

	[Header("Settings")]
	[SerializeField] private float _floorThickness = 0.1f;

	public int Width => _width;
	public int Height => _height;
	public List<MonsterEntry> PlacedMonsters => _placedMonsters;
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
	/// 지정 좌표의 타일 타입을 반환한다. 범위 밖이면 Wall, 기본값은 Path.
	/// </summary>
	public ETileType GetTile(int x, int z)
	{
		if (x < 0 || x >= _width || z < 0 || z >= _height) return ETileType.Wall;
		foreach (var e in _tileOverrides)
			if (e.pos.x == x && e.pos.y == z) return e.type;
		return ETileType.Path;
	}

	private int GetBlockIndex(int x, int z)
	{
		foreach (var b in _placedBlocks)
			if (b.pos.x == x && b.pos.y == z) return b.prefabIndex;
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
		if (GetBlockIndex(grid.x, grid.y) >= 0) return false;
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

		Transform container = new GameObject(VISUALS_CONTAINER).transform;
		container.SetParent(transform);
		container.localPosition = Vector3.zero;

		for (int z = 0; z < _height; z++)
		{
			for (int x = 0; x < _width; x++)
			{
				ETileType tileType = GetTile(x, z);
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

				// Spike 타일에 프리팹 배치
				if (tileType == ETileType.Spike && _spikePrefab != null)
				{
					GameObject spike = Instantiate(_spikePrefab, container);
					spike.name = $"Spike_{x}_{z}";
					spike.transform.position = tileCenter + Vector3.up * _blockYOffset;
					if (spike.GetComponent<SpikeTile>() == null)
						spike.AddComponent<SpikeTile>();
				}

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

				// Wall 타일에 물리 차단용 콜라이더 추가
				if (tileType == ETileType.Wall)
				{
					GameObject wallCollider = new GameObject("WallCollider");
					wallCollider.transform.SetParent(container);
					wallCollider.transform.position = tileCenter + Vector3.up * 1f;
					BoxCollider bc = wallCollider.AddComponent<BoxCollider>();
					bc.size = new Vector3(1f, 2f, 1f);
					bc.isTrigger = false;
					wallCollider.AddComponent<BlockObstacle>();
				}

				// Block prefab
				int blockIdx = GetBlockIndex(x, z);
				if (blockIdx >= 0 && _blockPrefabs != null && blockIdx < _blockPrefabs.Length && _blockPrefabs[blockIdx] != null)
				{
					GameObject block = Instantiate(_blockPrefabs[blockIdx], container);
					block.name = $"Block_{x}_{z}";
					block.transform.position = tileCenter + Vector3.up * _blockYOffset;
					block.AddComponent<BlockObstacle>();
				}
			}
		}

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
		wall.AddComponent<BlockObstacle>();
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
