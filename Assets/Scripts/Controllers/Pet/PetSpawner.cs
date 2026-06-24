using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// Player에 부착. EquipmentManager.OnEquipChanged를 구독하여
/// Pet1/Pet2 슬롯 변경 시 펫을 스폰/디스폰한다.
/// </summary>
public class PetSpawner : MonoBehaviour
{
	private Dictionary<EEquipSlot, PetController> _activePets = new();

	private void OnEnable()
	{
		if (PlayerController.Instance == null || PlayerController.Instance.gameObject != gameObject)
			return;
		EquipmentManager.Instance.OnEquipChanged += RefreshPets;
		RefreshPets();
	}

	private void OnDisable()
	{
		if (EquipmentManager.Instance != null)
			EquipmentManager.Instance.OnEquipChanged -= RefreshPets;
	}

	/// <summary>
	/// Pet1/Pet2 슬롯 상태를 확인하고 펫을 스폰/디스폰한다.
	/// </summary>
	private void RefreshPets()
	{
		RefreshSlot(EEquipSlot.Pet1);
		RefreshSlot(EEquipSlot.Pet2);
	}

	private void RefreshSlot(EEquipSlot slot)
	{
		var equipped = EquipmentManager.Instance.Equipped;
		bool hasEquip = equipped.TryGetValue(slot, out EquipmentData data);

		if (hasEquip)
		{
			EquipmentTable table = EquipmentDatabase.GetById(data.tableId);
			if (table == null) return;

			if (_activePets.TryGetValue(slot, out PetController existing) && existing != null)
			{
				if (existing.TableId == data.tableId)
				{
					existing.RefreshStats();
					return;
				}
				DespawnPet(slot);
			}
			SpawnPet(slot, data, table);
		}
		else if (_activePets.ContainsKey(slot))
		{
			DespawnPet(slot);
		}
	}

	private void SpawnPet(EEquipSlot slot, EquipmentData data, EquipmentTable table)
	{
		if (string.IsNullOrEmpty(table.prefab)) return;
		GameObject prefab = Resources.Load<GameObject>(table.prefab);
		if (prefab == null) return;

		GameObject go = Instantiate(prefab);
		PetController pet = go.GetComponent<PetController>();
		if (pet == null)
			pet = go.AddComponent<PetController>();
		pet.Init(data, table, slot);
		_activePets[slot] = pet;
	}

	/// <summary>
	/// 모든 활성 펫을 플레이어 offset 위치로 즉시 이동시킨다.
	/// </summary>
	public void TeleportPetsToOffset()
	{
		foreach (var pet in _activePets.Values)
			if (pet != null) pet.TeleportToOffset();
	}

	private void DespawnPet(EEquipSlot slot)
	{
		if (_activePets.TryGetValue(slot, out PetController pet) && pet != null)
		{
			pet.TransitionTo(null);
			Destroy(pet.gameObject);
		}
		_activePets.Remove(slot);
	}
}
