using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
	[SerializeField] private List<GameObject> _stageTileMapPrefabs;
	[SerializeField] private GameObject _playerPrefab;

	private GameObject _currentTileMapInstance;
	private TileMap _tileMap;
	public TileMap TileMap => _tileMap;

	[Header("Exp Orb")]
	[SerializeField] private GameObject _expOrbPrefab;

	[Header("Stage Transition")]
	[SerializeField] private float _fadeDuration = 0.5f;

	[Header("HP Heart")]
	[SerializeField] private GameObject _hpHeartPrefab;
	[SerializeField] private float _hpHeartDropChance = 0.15f;

	public int CurrentStageIndex => _currentStageIndex;
	public int TotalStageCount => _stageTileMapPrefabs?.Count ?? 0;
	public event Action OnExitDoorOpened;

	private GameObject _exitDoor;
	private int _currentStageIndex = 0;
	private List<MonsterBase> _aliveMonsters = new List<MonsterBase>();
	private List<ExpOrb> _spawnedExpOrbs = new List<ExpOrb>();
	private List<HPHeart> _spawnedHpHearts = new List<HPHeart>();

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

		// 남은 ExpOrb 정리
		foreach (var orb in _spawnedExpOrbs)
			if (orb != null) Destroy(orb.gameObject);
		_spawnedExpOrbs.Clear();

		// 남은 HPHeart 정리
		foreach (var heart in _spawnedHpHearts)
			if (heart != null) Destroy(heart.gameObject);
		_spawnedHpHearts.Clear();

		// 새 TileMap 생성
		_currentTileMapInstance = Instantiate(_stageTileMapPrefabs[index]);
		_tileMap = _currentTileMapInstance.GetComponent<TileMap>();
		_tileMap.GenerateVisuals();
		_exitDoor = _tileMap.SpawnedExitDoor;

		// 플레이어 이동
		TeleportPlayerToSpawn();

		// TileMap 몬스터 스폰
		if (_tileMap.MonsterPrefabs != null)
		{
			foreach (var entry in _tileMap.PlacedMonsters)
			{
				if (entry.prefabIndex < 0 || entry.prefabIndex >= _tileMap.MonsterPrefabs.Length) continue;
				GameObject go = Instantiate(_tileMap.MonsterPrefabs[entry.prefabIndex],
					_tileMap.GridToWorld(entry.pos.x, entry.pos.y), Quaternion.LookRotation(Vector3.back));
				MonsterBase mb = go.GetComponent<MonsterBase>();
				if (mb != null) _aliveMonsters.Add(mb);
			}
		}

		// 플레이어가 WallPass/WaterWalker를 보유하면 새 타일에도 적용
		PlayerController player = FindAnyObjectByType<PlayerController>();
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
	/// 몬스터 사망 시 호출. 피의갈증 회복, HP하트 드롭, ExpOrb 스폰.
	/// </summary>
	public void OnMonsterDead(MonsterBase monster)
	{
		_aliveMonsters.Remove(monster);
		SpawnExpOrb(monster.transform.position, monster.ExpReward);

		// 피의갈증: 처치 시 HP 회복
		PlayerController player = FindAnyObjectByType<PlayerController>();
		if (player != null)
		{
			PlayerUpgrade upgrade = player.GetComponent<PlayerUpgrade>();
			if (upgrade != null && upgrade.BloodThirstHeal > 0f)
				player.Heal(upgrade.BloodThirstHeal);
		}

		// HP하트 드롭
		SpawnHpHeart(monster.transform.position);

		if (_aliveMonsters.Count == 0)
		{
			CollectAllExpOrbs();
			CollectAllHpHearts();
			OpenExitDoor();
		}
	}

	/// <summary>
	/// 출구 문을 활성화한다.
	/// </summary>
	private void OpenExitDoor()
	{
		StartCoroutine(OpenExitDoorSequence());
	}

	private IEnumerator OpenExitDoorSequence()
	{
		yield return new WaitForSeconds(1.5f);

		if (_exitDoor == null) yield break;
		ExitDoor door = _exitDoor.GetComponent<ExitDoor>();
		if (door != null)
			door.OpenDoor();

		OnExitDoorOpened?.Invoke();
	}

	/// <summary>
	/// 대기 중인 모든 ExpOrb에 수집 시작을 알린다.
	/// </summary>
	private void CollectAllExpOrbs()
	{
		foreach (var orb in _spawnedExpOrbs)
			if (orb != null) orb.StartCollect();
	}

	/// <summary>
	/// 대기 중인 모든 HPHeart에 수집 시작을 알린다.
	/// </summary>
	private void CollectAllHpHearts()
	{
		foreach (var heart in _spawnedHpHearts)
			if (heart != null) heart.StartCollect();
	}

	/// <summary>
	/// 지정 위치에 경험치 오브를 하나 생성한다.
	/// </summary>
	private void SpawnExpOrb(Vector3 pos, int exp)
	{
		if (_expOrbPrefab != null)
		{
			GameObject orb = Instantiate(_expOrbPrefab, pos, Quaternion.identity);
			ExpOrb expOrb = orb.GetComponent<ExpOrb>();
			if (expOrb != null)
			{
				expOrb.Init(exp);
				_spawnedExpOrbs.Add(expOrb);
			}
		}
		else
		{
			ExpManager.Instance.AddExp(exp);
		}
	}

	/// <summary>
	/// 확률적으로 HP하트를 드롭한다.
	/// </summary>
	private void SpawnHpHeart(Vector3 pos)
	{
		if (_hpHeartPrefab == null) return;
		if (UnityEngine.Random.value > _hpHeartDropChance) return;

		GameObject go = Instantiate(_hpHeartPrefab, pos, Quaternion.identity);
		HPHeart heart = go.GetComponent<HPHeart>();
		if (heart != null)
		{
			heart.Init();
			_spawnedHpHearts.Add(heart);
		}
	}

	/// <summary>
	/// 다음 스테이지로 진행한다. 페이드 전환 효과를 적용한다.
	/// </summary>
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
}
