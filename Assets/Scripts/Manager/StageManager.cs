using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
	[SerializeField] private List<GameObject> _stageTileMapPrefabs;
	[SerializeField] private GameObject _playerPrefab;

	private GameObject _currentTileMapInstance;
	private TileMap _tileMap;
	public TileMap TileMap => _tileMap;

	private GameObject _exitDoor;
	private int _currentStageIndex = 0;
	private List<MonsterBase> _aliveMonsters = new List<MonsterBase>();

	private void Start()
	{
		SpawnPlayer();
		if (_stageTileMapPrefabs != null && _stageTileMapPrefabs.Count > 0)
			StartStage(0);
	}

	/// <summary>
	/// 플레이어를 origin에 임시 스폰한다. StartStage에서 스폰 포인트로 이동된다.
	/// </summary>
	private void SpawnPlayer()
	{
		if (_playerPrefab == null) return;
		Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
	}

	/// <summary>
	/// 해당 인덱스의 스테이지를 시작하고 TileMap을 교체한다.
	/// </summary>
	public void StartStage(int index)
	{
		_currentStageIndex = index;

		if (index >= _stageTileMapPrefabs.Count)
		{
			Debug.Log("All stages cleared!");
			return;
		}

		// 이전 TileMap 삭제
		if (_currentTileMapInstance != null)
			Destroy(_currentTileMapInstance);

		// 살아있는 몬스터 정리
		foreach (var m in _aliveMonsters)
			if (m != null) Destroy(m.gameObject);
		_aliveMonsters.Clear();

		// 새 TileMap 생성
		_currentTileMapInstance = Instantiate(_stageTileMapPrefabs[index]);
		_tileMap = _currentTileMapInstance.GetComponent<TileMap>();
		_tileMap.GenerateVisuals();
		_exitDoor = _tileMap.SpawnedExitDoor;
		if (_exitDoor != null) _exitDoor.SetActive(false);

		// 플레이어 이동
		TeleportPlayerToSpawn();

		// MapData 몬스터 스폰
		if (_tileMap.MapData != null && _tileMap.MonsterPrefabs != null)
		{
			MapData md = _tileMap.MapData;
			for (int z = 0; z < md.height; z++)
				for (int x = 0; x < md.width; x++)
				{
					int mi = md.GetMonsterIndex(x, z);
					if (mi < 0 || mi >= _tileMap.MonsterPrefabs.Length) continue;
					GameObject go = Instantiate(_tileMap.MonsterPrefabs[mi],
						_tileMap.GridToWorld(x, z), Quaternion.identity);
					MonsterBase mb = go.GetComponent<MonsterBase>();
					if (mb != null) _aliveMonsters.Add(mb);
				}
		}

		if (_aliveMonsters.Count == 0)
			OpenExitDoor();
	}

	/// <summary>
	/// 플레이어를 현재 TileMap의 스폰 포인트로 순간이동한다.
	/// </summary>
	private void TeleportPlayerToSpawn()
	{
		if (_tileMap == null) return;
		PlayerController player = FindAnyObjectByType<PlayerController>();
		if (player == null) return;
		Vector3 spawnPos = _tileMap.GetSpawnWorldPosition();
		CharacterController cc = player.GetComponent<CharacterController>();
		if (cc != null) cc.enabled = false;
		player.transform.position = spawnPos;
		if (cc != null) cc.enabled = true;
	}

	/// <summary>
	/// 몬스터 사망 시 호출. 모든 몬스터가 죽으면 출구를 연다.
	/// </summary>
	public void OnMonsterDead(MonsterBase monster)
	{
		_aliveMonsters.Remove(monster);
		if (_aliveMonsters.Count == 0)
			OpenExitDoor();
	}

	/// <summary>
	/// 출구 문을 활성화한다.
	/// </summary>
	private void OpenExitDoor()
	{
		if (_exitDoor == null) return;
		_exitDoor.SetActive(true);
	}

	/// <summary>
	/// 다음 스테이지로 진행한다.
	/// </summary>
	public void GoToNextStage()
	{
		StartStage(_currentStageIndex + 1);
	}
}
