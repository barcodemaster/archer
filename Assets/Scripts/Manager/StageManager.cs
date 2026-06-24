using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스테이지 전환과 초기화만 담당한다.
/// 보상은 RewardManager, 난이도는 DifficultyManager로 분리(SRP).
/// </summary>
public class StageManager : Singleton<StageManager>
{
	[Header("Stage Prefabs")]
	[SerializeField] private List<GameObject> _normalStages;
	[SerializeField] private List<GameObject> _bossStagePrefabs;
	[SerializeField] private GameObject _playerPrefab;
	[SerializeField] private List<GameObject> _angelRoomPrefabs;
	[SerializeField] private GameObject _startStagePrefab;

	private List<GameObject> _stagePlayOrder;
	private HashSet<GameObject> _angelRoomSet;

	private GameObject _currentTileMapInstance;
	private TileMap _tileMap;
	public TileMap TileMap => _tileMap;

	[Header("Stage Transition")]
	[SerializeField] private float _fadeDuration = 0.5f;

	[Header("Intro Drop")]
	[SerializeField] private float _introDropHeight = 8f;
	[SerializeField] private float _introDropDuration = 0.6f;
	[SerializeField] private float _introRouletteDelay = 1.0f;

	public int CurrentStageIndex => _currentStageIndex;
	public int AliveCost => DifficultyManager.Instance.GetAliveCost(_currentStageIndex);
	public int TotalStageCount => _stagePlayOrder?.Count ?? 0;
	public IReadOnlyList<MonsterBase> AliveMonsters => _aliveMonsters;
	public event Action OnExitDoorOpened;

	private GameObject _exitDoor;
	private int _currentStageIndex = 0;
	private List<MonsterBase> _aliveMonsters = new List<MonsterBase>();

	private void Start()
	{
		SaveManager.Load();
		SpawnPlayer();
		BuildStagePlayOrder();
		if (_stagePlayOrder != null && _stagePlayOrder.Count > 0)
		{
			StartStage(0);
			StartCoroutine(IntroDropSequence());
		}
	}

	/// <summary>
	/// 스테이지 플레이 순서를 랜덤으로 생성한다.
	/// 패턴: 시작 + (일반4 + 천사 + 일반4 + 보스) x 보스 수
	/// </summary>
	private void BuildStagePlayOrder()
	{
		_stagePlayOrder = new List<GameObject>();
		_angelRoomSet = new HashSet<GameObject>();
		if (_angelRoomPrefabs != null)
			foreach (var prefab in _angelRoomPrefabs)
				if (prefab != null) _angelRoomSet.Add(prefab);

		_stagePlayOrder.Add(_startStagePrefab);

		int bossCount = _bossStagePrefabs != null ? _bossStagePrefabs.Count : 0;
		int stagesPerZone = 8;

		for (int i = 0; i < bossCount; i++)
		{
			List<GameObject> zoneStages = GetZoneStages(i, stagesPerZone);
			Shuffle(zoneStages);

			for (int j = 0; j < 4 && j < zoneStages.Count; j++)
				_stagePlayOrder.Add(zoneStages[j]);
			_stagePlayOrder.Add(PickRandomAngelRoom());
			for (int j = 4; j < 8 && j < zoneStages.Count; j++)
				_stagePlayOrder.Add(zoneStages[j]);
			_stagePlayOrder.Add(_bossStagePrefabs[i]);
		}
	}

	private GameObject PickRandomAngelRoom()
	{
		if (_angelRoomPrefabs == null || _angelRoomPrefabs.Count == 0) return null;
		return _angelRoomPrefabs[UnityEngine.Random.Range(0, _angelRoomPrefabs.Count)];
	}

	public bool IsStartStage(int index)
	{
		if (index < 0 || index >= _stagePlayOrder.Count) return false;
		return _startStagePrefab != null && _stagePlayOrder[index] == _startStagePrefab;
	}

	public bool IsAngelRoom(int index)
	{
		if (_angelRoomSet == null || index < 0 || index >= _stagePlayOrder.Count) return false;
		return _angelRoomSet.Contains(_stagePlayOrder[index]);
	}

	public void OnAngelBlessingSelected() => OpenExitDoor();
	public void OnIntroCompleted() => OpenExitDoor();

	private List<GameObject> GetZoneStages(int zoneIndex, int stagesPerZone)
	{
		var result = new List<GameObject>();
		int start = zoneIndex * stagesPerZone;
		for (int i = start; i < start + stagesPerZone && i < _normalStages.Count; i++)
			result.Add(_normalStages[i]);
		return result;
	}

	private void Shuffle<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = UnityEngine.Random.Range(0, i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}

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

		if (index >= _stagePlayOrder.Count)
		{
			Logger.Log("StageManager", "All stages cleared!");
			return;
		}

		// 이전 TileMap 삭제
		if (_currentTileMapInstance != null)
			Destroy(_currentTileMapInstance);

		// 살아있는 몬스터 정리
		foreach (var m in _aliveMonsters)
			if (m != null) Destroy(m.gameObject);
		_aliveMonsters.Clear();

		// 남은 보상 정리 (RewardManager에 위임)
		RewardManager.Instance.CleanupAll();

		// 날아가는 발사체 정리
		ProjectileBase[] projectiles = FindObjectsByType<ProjectileBase>(FindObjectsSortMode.None);
		foreach (var p in projectiles)
			ObjectPool.Instance.Return(p.gameObject);

		// 새 TileMap 생성
		_currentTileMapInstance = Instantiate(_stagePlayOrder[index]);
		_tileMap = _currentTileMapInstance.GetComponent<TileMap>();
		_tileMap.GenerateVisuals();
		_exitDoor = _tileMap.SpawnedExitDoor;

		if (CameraController.Instance != null)
			CameraController.Instance.SetMapCenter(_tileMap);

		TeleportPlayerToSpawn();

		// TileMap 몬스터 스폰 (DifficultyManager에 위임)
		if (_tileMap.MonsterPrefabs != null)
		{
			foreach (var entry in _tileMap.PlacedMonsters)
			{
				if (entry.prefabIndex < 0 || entry.prefabIndex >= _tileMap.MonsterPrefabs.Length) continue;
				GameObject go = Instantiate(_tileMap.MonsterPrefabs[entry.prefabIndex],
					_tileMap.GridToWorld(entry.pos.x, entry.pos.y), Quaternion.LookRotation(Vector3.back));
				MonsterBase mb = go.GetComponent<MonsterBase>();
				if (mb != null)
				{
					DifficultyManager.Instance.ApplyToMonster(mb, _currentStageIndex);
					_aliveMonsters.Add(mb);
				}
			}
		}

		// 플레이어 타일 패스 적용 (캐싱된 인스턴스 사용)
		PlayerController player = PlayerController.Instance;
		if (player != null)
		{
			PlayerUpgrade upgrade = player.GetComponent<PlayerUpgrade>();
			if (upgrade != null)
			{
				if (upgrade.GetLevel(Define.EUpgradeType.WallPass) > 0)
					player.ApplyWallPassCollisions();
				if (upgrade.GetLevel(Define.EUpgradeType.WaterWalker) > 0)
					player.ApplyWaterWalkCollisions();
			}
		}

		// 천사방이면 AngelNPC 드롭 연출
		if (IsAngelRoom(index))
		{
			AngelNPC angel = FindAnyObjectByType<AngelNPC>();
			if (angel != null)
				angel.PlayDropAnimation();
		}

		if (_aliveMonsters.Count == 0 && !IsAngelRoom(index) && !IsStartStage(index))
			OpenExitDoor();
	}

	private void TeleportPlayerToSpawn()
	{
		if (_tileMap == null) return;
		PlayerController player = PlayerController.Instance;
		if (player == null) return;
		Vector3 spawnPos = _tileMap.GetSpawnWorldPosition();
		CharacterController cc = player.GetComponent<CharacterController>();
		if (cc != null) cc.enabled = false;
		player.transform.position = spawnPos;
		if (cc != null) cc.enabled = true;

		PetSpawner spawner = player.GetComponent<PetSpawner>();
		if (spawner != null) spawner.TeleportPetsToOffset();
	}

	private IEnumerator IntroDropSequence()
	{
		PlayerController player = PlayerController.Instance;
		if (player == null) yield break;

		CharacterController cc = player.GetComponent<CharacterController>();
		player.IsIntroDrop = true;

		Vector3 spawnPos = player.transform.position;
		Vector3 startPos = spawnPos + Vector3.up * _introDropHeight;

		if (cc != null) cc.enabled = false;
		player.transform.position = startPos;
		if (cc != null) cc.enabled = true;

		float elapsed = 0f;
		while (elapsed < _introDropDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / _introDropDuration);
			float eased = t * t;
			Vector3 pos = Vector3.Lerp(startPos, spawnPos, eased);

			if (cc != null) cc.enabled = false;
			player.transform.position = pos;
			if (cc != null) cc.enabled = true;

			yield return null;
		}

		if (cc != null) cc.enabled = false;
		player.transform.position = spawnPos;
		if (cc != null) cc.enabled = true;

		AudioManager.Instance?.PlayLanding();
		player.IsIntroDrop = false;

		yield return new WaitForSeconds(_introRouletteDelay);

		UI_LevelUp levelUp = FindAnyObjectByType<UI_LevelUp>(FindObjectsInactive.Include);
		if (levelUp != null)
			levelUp.ShowIntro();
	}

	/// <summary>
	/// 몬스터 사망 시 호출. RewardManager에 보상 처리를 위임한다.
	/// </summary>
	public void OnMonsterDead(MonsterBase monster)
	{
		_aliveMonsters.Remove(monster);

		AchievementManager.Instance.AddProgress(Define.EAchievementType.KillCount);
		if (monster.IsBoss)
			AchievementManager.Instance.AddProgress(Define.EAchievementType.BossKill);

		RewardManager.Instance.DropMonsterRewards(monster, PlayerController.Instance);

		if (_aliveMonsters.Count == 0)
		{
			RewardManager.Instance.CollectAll();
			OpenExitDoor();
		}
	}

	public void RegisterMonster(MonsterBase monster)
	{
		if (!_aliveMonsters.Contains(monster))
			_aliveMonsters.Add(monster);
	}

	public List<EquipmentData> GetCollectedEquipments() => RewardManager.Instance.CollectedEquipments;
	public int GetTotalGoldCollected() => RewardManager.Instance.TotalGoldCollected;

	private void OpenExitDoor()
	{
		StartCoroutine(OpenExitDoorSequence());
	}

	private IEnumerator OpenExitDoorSequence()
	{
		RewardManager.Instance.GrantStageGold();
		SaveManager.UpdateBestStage(_currentStageIndex);
		AchievementManager.Instance.SetProgress(Define.EAchievementType.StageClear, _currentStageIndex + 1);

		if (_exitDoor == null) yield break;
		ExitDoor door = _exitDoor.GetComponent<ExitDoor>();
		if (door != null)
			door.OpenDoor();

		OnExitDoorOpened?.Invoke();
	}

	public void GoToNextStage()
	{
		StartCoroutine(StageTransitionSequence(_currentStageIndex + 1));
	}

	private IEnumerator StageTransitionSequence(int nextIndex)
	{
		yield return UIManager.Instance.FadeOut(_fadeDuration);
		UIManager.Instance.HideStageProgress();
		StartStage(nextIndex);
		yield return UIManager.Instance.FadeIn(_fadeDuration);
	}

#if UNITY_EDITOR
	[Header("Cheat (Editor Only)")]
	[SerializeField] private bool _useCheat = false;
	[SerializeField] private int _cheatStageIndex = 0;
	[SerializeField] private List<int> _cheatStageSequence = new List<int>();

	private void Update()
	{
		if (!_useCheat || Keyboard.current == null) return;

		if (Keyboard.current.f5Key.wasPressedThisFrame)
			CheatGoToStage(_cheatStageIndex);

		if (Keyboard.current.f6Key.wasPressedThisFrame)
			CheatPlaySequence();
	}

	/// <summary>
	/// 지정한 스테이지 인덱스로 즉시 이동한다 (에디터 전용 치트).
	/// </summary>
	private void CheatGoToStage(int index)
	{
		var sequential = BuildSequentialList();

		if (index < 0 || index >= sequential.Count)
		{
			Logger.LogWarning("StageManager", $"Cheat: Invalid stage index {index} (total: {sequential.Count})");
			return;
		}

		// 해당 프리팹을 _stagePlayOrder의 다음 위치에 삽입
		int insertAt = _currentStageIndex + 1;
		if (insertAt >= _stagePlayOrder.Count)
			_stagePlayOrder.Add(sequential[index]);
		else
			_stagePlayOrder[insertAt] = sequential[index];
		StartCoroutine(StageTransitionSequence(insertAt));
	}

	/// <summary>
	/// 지정한 스테이지 시퀀스로 플레이 순서를 재구성한다 (에디터 전용 치트).
	/// </summary>
	private void CheatPlaySequence()
	{
		if (_cheatStageSequence == null || _cheatStageSequence.Count == 0)
		{
			Logger.LogWarning("StageManager", "Cheat: Stage sequence is empty");
			return;
		}

		var sequential = BuildSequentialList();

		var newOrder = new List<GameObject>();
		foreach (int idx in _cheatStageSequence)
		{
			if (idx < 0 || idx >= sequential.Count)
			{
				Logger.LogWarning("StageManager", $"Cheat: Invalid index {idx} in sequence (total: {sequential.Count}), skipping");
				continue;
			}
			newOrder.Add(sequential[idx]);
		}

		if (newOrder.Count == 0)
		{
			Logger.LogWarning("StageManager", "Cheat: No valid stages in sequence");
			return;
		}

		_stagePlayOrder = newOrder;
		StartCoroutine(StageTransitionSequence(0));
	}

	/// <summary>
	/// 셔플 없는 순차 스테이지 목록을 빌드한다 (치트용 공통).
	/// </summary>
	private List<GameObject> BuildSequentialList()
	{
		var sequential = new List<GameObject>();
		sequential.Add(_startStagePrefab);
		int bossCount = _bossStagePrefabs != null ? _bossStagePrefabs.Count : 0;
		int stagesPerZone = 8;
		for (int i = 0; i < bossCount; i++)
		{
			List<GameObject> zoneStages = GetZoneStages(i, stagesPerZone);
			for (int j = 0; j < 4 && j < zoneStages.Count; j++)
				sequential.Add(zoneStages[j]);
			sequential.Add(PickRandomAngelRoom());
			for (int j = 4; j < 8 && j < zoneStages.Count; j++)
				sequential.Add(zoneStages[j]);
			sequential.Add(_bossStagePrefabs[i]);
		}
		return sequential;
	}
#endif
}
