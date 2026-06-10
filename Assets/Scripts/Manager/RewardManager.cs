using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보상 아이템(ExpOrb, HPHeart, GoldOrb, EquipmentOrb)의 스폰과 수집을 관리한다.
/// SRP 원칙에 따라 StageManager에서 분리.
/// </summary>
public class RewardManager : Singleton<RewardManager>
{
	[Header("Prefabs")]
	[SerializeField] private GameObject _expOrbPrefab;
	[SerializeField] private GameObject _hpHeartPrefab;
	[SerializeField] private GameObject _goldOrbPrefab;
	[SerializeField] private GameObject _equipmentOrbPrefab;

	protected override void Awake()
	{
		base.Awake();
		LoadPrefabsIfNeeded();
	}

	/// <summary>
	/// SerializeField가 null인 경우 Resources에서 프리팹을 로드한다.
	/// Singleton 자동 생성 시 Inspector 할당이 없으므로 fallback으로 사용.
	/// </summary>
	private void LoadPrefabsIfNeeded()
	{
		if (_expOrbPrefab == null)
			_expOrbPrefab = Resources.Load<GameObject>("Prefabs/ExpOrb");
		if (_hpHeartPrefab == null)
			_hpHeartPrefab = Resources.Load<GameObject>("Prefabs/Heart");
		if (_goldOrbPrefab == null)
			_goldOrbPrefab = Resources.Load<GameObject>("Prefabs/GoldOrb");
		if (_equipmentOrbPrefab == null)
			_equipmentOrbPrefab = Resources.Load<GameObject>("Prefabs/EquipmenetOrb");
	}

	private List<ExpOrb> _spawnedExpOrbs = new();
	private List<HPHeart> _spawnedHpHearts = new();
	private List<GoldOrb> _spawnedGoldOrbs = new();
	private List<EquipmentOrb> _spawnedEquipmentOrbs = new();
	private List<EquipmentData> _collectedEquipments = new();
	private int _totalGoldCollected;

	public List<EquipmentData> CollectedEquipments => _collectedEquipments;
	public int TotalGoldCollected => _totalGoldCollected;

	/// <summary>
	/// 몬스터 사망 시 보상을 드롭한다.
	/// </summary>
	public void DropMonsterRewards(MonsterBase monster, PlayerController player)
	{
		Vector3 basePos = monster.transform.position;

		SpawnExpOrb(basePos + GetRandomDropOffset(), monster.ExpReward);

		// 피의갈증: 처치 시 HP 회복
		if (player != null)
		{
			PlayerUpgrade upgrade = player.GetComponent<PlayerUpgrade>();
			if (upgrade != null && upgrade.BloodThirstHealPercent > 0f)
				player.Heal(player.MaxHp * upgrade.BloodThirstHealPercent);
		}

		SpawnHpHeart(basePos + GetRandomDropOffset());
		SpawnGoldOrb(basePos + GetRandomDropOffset());
		SpawnEquipmentOrb(basePos + GetRandomDropOffset());
	}

	/// <summary>
	/// 스테이지 클리어 시 모든 보상을 수집한다.
	/// </summary>
	public void CollectAll()
	{
		foreach (var orb in _spawnedExpOrbs)
			if (orb != null) orb.StartCollect();
		foreach (var heart in _spawnedHpHearts)
			if (heart != null) heart.StartCollect();
		foreach (var orb in _spawnedGoldOrbs)
			if (orb != null) orb.StartCollect();
		foreach (var orb in _spawnedEquipmentOrbs)
			if (orb != null) orb.StartCollect();
	}

	/// <summary>
	/// 스테이지 전환 시 남은 보상 오브를 모두 정리한다.
	/// </summary>
	public void CleanupAll()
	{
		CleanupList(_spawnedExpOrbs);
		CleanupList(_spawnedHpHearts);
		CleanupList(_spawnedGoldOrbs);
		CleanupList(_spawnedEquipmentOrbs);
	}

	/// <summary>
	/// 스테이지 클리어 골드 보상을 지급한다.
	/// </summary>
	public void GrantStageGold()
	{
		int reward = GameConfig.Instance.stageGoldReward;
		_totalGoldCollected += reward;
		GoldManager.Instance.AddGold(reward);
	}

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

	private void SpawnHpHeart(Vector3 pos)
	{
		if (_hpHeartPrefab == null) return;
		if (Random.value > GameConfig.Instance.hpHeartDropChance) return;

		GameObject go = Instantiate(_hpHeartPrefab, pos, Quaternion.identity);
		HPHeart heart = go.GetComponent<HPHeart>();
		if (heart != null)
		{
			heart.Init();
			_spawnedHpHearts.Add(heart);
		}
	}

	private void SpawnGoldOrb(Vector3 pos)
	{
		int gold = Random.Range(GameConfig.Instance.monsterGoldMin, GameConfig.Instance.monsterGoldMax + 1);
		_totalGoldCollected += gold;

		if (_goldOrbPrefab == null)
		{
			GoldManager.Instance.AddGold(gold);
			return;
		}

		GameObject go = Instantiate(_goldOrbPrefab, pos, Quaternion.identity);
		GoldOrb orb = go.GetComponent<GoldOrb>();
		if (orb != null)
		{
			orb.Init(gold);
			_spawnedGoldOrbs.Add(orb);
		}
	}

	private void SpawnEquipmentOrb(Vector3 pos)
	{
		if (_equipmentOrbPrefab == null) return;
		if (Random.value > GameConfig.Instance.equipmentDropChance) return;

		EquipmentData item = EquipmentManager.Instance.CreateRandomItem();
		if (item == null) return;

		_collectedEquipments.Add(item);
		GameObject go = Instantiate(_equipmentOrbPrefab, pos, Quaternion.identity);
		EquipmentOrb orb = go.GetComponent<EquipmentOrb>();
		if (orb != null)
		{
			orb.Init(item);
			_spawnedEquipmentOrbs.Add(orb);
		}
	}

	private Vector3 GetRandomDropOffset()
	{
		Vector2 offset = Random.insideUnitCircle * 0.5f;
		return new Vector3(offset.x, 0f, offset.y);
	}

	private void CleanupList<T>(List<T> list) where T : Component
	{
		foreach (var item in list)
			if (item != null) Destroy(item.gameObject);
		list.Clear();
	}
}
