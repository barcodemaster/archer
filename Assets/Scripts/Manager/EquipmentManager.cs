using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 장비 인벤토리 및 장착을 관리하는 싱글톤 매니저.
/// </summary>
public class EquipmentManager : Singleton<EquipmentManager>
{
	private List<EquipmentData> _inventory = new List<EquipmentData>();
	private Dictionary<EEquipSlot, EquipmentData> _equipped = new Dictionary<EEquipSlot, EquipmentData>();

	public List<EquipmentData> Inventory => _inventory;
	public Dictionary<EEquipSlot, EquipmentData> Equipped => _equipped;

	public event Action OnInventoryChanged;
	public event Action OnEquipChanged;

	/// <summary>
	/// 인벤토리에 아이템을 추가한다.
	/// </summary>
	public void AddItem(EquipmentData item)
	{
		_inventory.Add(item);
		AchievementManager.Instance.AddProgress(Define.EAchievementType.EquipmentCollected);
		OnInventoryChanged?.Invoke();
		SaveManager.Save();
	}

	/// <summary>
	/// 아이템을 슬롯에 장착한다. 기존 장비는 자동 해제.
	/// </summary>
	public bool Equip(EquipmentData item, EEquipSlot slot)
	{
		if (!IsSlotCompatible(item, slot)) return false;

		// 기존 장비 해제
		if (_equipped.ContainsKey(slot))
		{
			var old = _equipped[slot];
			old.isEquipped = false;
		}

		item.isEquipped = true;
		item.equippedSlot = slot;
		_equipped[slot] = item;

		OnEquipChanged?.Invoke();
		SaveManager.Save();
		return true;
	}

	/// <summary>
	/// 슬롯의 장비를 해제한다.
	/// </summary>
	public void Unequip(EEquipSlot slot)
	{
		if (!_equipped.ContainsKey(slot)) return;

		var item = _equipped[slot];
		item.isEquipped = false;
		_equipped.Remove(slot);

		OnEquipChanged?.Invoke();
		SaveManager.Save();
	}

	/// <summary>
	/// 아이템 레벨을 올린다. 최대 50.
	/// </summary>
	public bool LevelUp(EquipmentData item)
	{
		if (item.level >= 50) return false;

		EquipmentTable table = EquipmentDatabase.GetById(item.tableId);
		if (table == null) return false;

		int cost = item.GetLevelUpCost(table);
		if (!GoldManager.Instance.SpendGold(cost)) return false;

		item.level++;
		OnInventoryChanged?.Invoke();
		if (item.isEquipped)
			OnEquipChanged?.Invoke();
		SaveManager.Save();
		return true;
	}

	/// <summary>
	/// 랜덤 장비 인스턴스를 생성한다.
	/// </summary>
	public EquipmentData CreateRandomItem()
	{
		EquipmentTable table = EquipmentDatabase.PickRandomDrop();
		if (table == null) return null;

		ESubStatType[] subTypes = (ESubStatType[])System.Enum.GetValues(typeof(ESubStatType));
		ESubStatType subType = subTypes[UnityEngine.Random.Range(0, subTypes.Length)];
		float subValue = UnityEngine.Random.Range(table.baseSubMin, table.baseSubMax);

		return new EquipmentData
		{
			uid = System.Guid.NewGuid().ToString(),
			tableId = table.id,
			level = 1,
			subStatType = subType,
			subStatValue = Mathf.Round(subValue * 10f) / 10f,
			isEquipped = false,
		};
	}

	/// <summary>
	/// 슬롯 호환성을 확인한다. Ring은 Ring1/Ring2, Pet은 Pet1/Pet2만 가능.
	/// </summary>
	public bool IsSlotCompatible(EquipmentData item, EEquipSlot slot)
	{
		EquipmentTable table = EquipmentDatabase.GetById(item.tableId);
		if (table == null) return false;

		EEquipSlot tableSlot = table.slot;

		// Ring: Ring1 테이블 아이템은 Ring1/Ring2 슬롯에 장착 가능
		if (tableSlot == EEquipSlot.Ring1)
			return slot == EEquipSlot.Ring1 || slot == EEquipSlot.Ring2;

		// Pet: Pet1 테이블 아이템은 Pet1/Pet2 슬롯에 장착 가능
		if (tableSlot == EEquipSlot.Pet1)
			return slot == EEquipSlot.Pet1 || slot == EEquipSlot.Pet2;

		return tableSlot == slot;
	}

	/// <summary>
	/// 장착 장비의 총 공격력 보너스를 계산한다.
	/// </summary>
	public float GetTotalAttackBonus()
	{
		float total = 0f;
		foreach (var kvp in _equipped)
		{
			EquipmentTable table = EquipmentDatabase.GetById(kvp.Value.tableId);
			if (table != null && table.mainStatType == EMainStatType.Attack)
				total += kvp.Value.GetMainStat(table);
		}
		return total;
	}

	/// <summary>
	/// 장착 장비의 총 최대HP 보너스를 계산한다.
	/// </summary>
	public float GetTotalMaxHpBonus()
	{
		float total = 0f;
		foreach (var kvp in _equipped)
		{
			EquipmentTable table = EquipmentDatabase.GetById(kvp.Value.tableId);
			if (table != null && table.mainStatType == EMainStatType.MaxHP)
				total += kvp.Value.GetMainStat(table);
		}
		return total;
	}

	/// <summary>
	/// 장착 장비의 특정 서브스탯 합산을 계산한다.
	/// </summary>
	public float GetTotalSubStat(ESubStatType type)
	{
		float total = 0f;
		foreach (var kvp in _equipped)
		{
			if (kvp.Value.subStatType == type)
				total += kvp.Value.subStatValue;
		}
		return total;
	}

	/// <summary>
	/// 아이템에 맞는 빈 슬롯을 자동으로 찾는다.
	/// </summary>
	public EEquipSlot FindAvailableSlot(EquipmentData item)
	{
		EquipmentTable table = EquipmentDatabase.GetById(item.tableId);
		if (table == null) return EEquipSlot.Weapon;

		if (table.slot == EEquipSlot.Ring1)
		{
			if (!_equipped.ContainsKey(EEquipSlot.Ring1)) return EEquipSlot.Ring1;
			if (!_equipped.ContainsKey(EEquipSlot.Ring2)) return EEquipSlot.Ring2;
			return EEquipSlot.Ring1;
		}

		if (table.slot == EEquipSlot.Pet1)
		{
			if (!_equipped.ContainsKey(EEquipSlot.Pet1)) return EEquipSlot.Pet1;
			if (!_equipped.ContainsKey(EEquipSlot.Pet2)) return EEquipSlot.Pet2;
			return EEquipSlot.Pet1;
		}

		return table.slot;
	}

	/// <summary>
	/// 세이브 데이터에서 로드한다.
	/// </summary>
	public void LoadFromSave(SaveData data)
	{
		_inventory.Clear();
		_equipped.Clear();

		if (data?.items == null) return;

		foreach (var entry in data.items)
		{
			var item = new EquipmentData
			{
				uid = entry.uid,
				tableId = entry.tableId,
				level = entry.level,
				subStatType = entry.subStatType,
				subStatValue = entry.subStatValue,
				isEquipped = entry.isEquipped,
				equippedSlot = entry.equippedSlot,
			};
			_inventory.Add(item);

			if (item.isEquipped)
				_equipped[item.equippedSlot] = item;
		}

		OnInventoryChanged?.Invoke();
		OnEquipChanged?.Invoke();
	}

	/// <summary>
	/// 현재 상태를 세이브 데이터로 변환한다.
	/// </summary>
	public SaveData ToSaveData()
	{
		var data = new SaveData();
		data.gold = GoldManager.Instance.Gold;
		data.items = new List<EquipmentSaveEntry>();

		foreach (var item in _inventory)
		{
			data.items.Add(new EquipmentSaveEntry
			{
				uid = item.uid,
				tableId = item.tableId,
				level = item.level,
				subStatType = item.subStatType,
				subStatValue = item.subStatValue,
				isEquipped = item.isEquipped,
				equippedSlot = item.equippedSlot,
			});
		}

		return data;
	}
}
