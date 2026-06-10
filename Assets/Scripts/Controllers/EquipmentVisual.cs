using UnityEngine;
using static Define;

/// <summary>
/// 무기 프리팹을 캐릭터 본에 부착하는 독립 컴포넌트.
/// </summary>
public class EquipmentVisual : MonoBehaviour
{
	[SerializeField] private bool _autoSubscribe = true;

	private Transform _weaponBone;
	private GameObject _weaponInstance;

	private void Awake()
	{
		_weaponBone = FindBoneRecursive(transform, "weapon_r");
	}

	private void OnEnable()
	{
		if (_autoSubscribe && EquipmentManager.Instance != null)
		{
			EquipmentManager.Instance.OnEquipChanged += RefreshWeapon;
			RefreshWeapon();
		}
	}

	private void OnDisable()
	{
		if (_autoSubscribe && EquipmentManager.Instance != null)
			EquipmentManager.Instance.OnEquipChanged -= RefreshWeapon;
	}

	/// <summary>
	/// 현재 장착된 무기에 맞는 프리팹을 본에 부착한다.
	/// </summary>
	public void RefreshWeapon()
	{
		if (_weaponInstance != null)
			Destroy(_weaponInstance);

		if (_weaponBone == null) return;

		var equipped = EquipmentManager.Instance.Equipped;
		if (!equipped.TryGetValue(EEquipSlot.Weapon, out EquipmentData weaponData))
			return;

		EquipmentTable table = EquipmentDatabase.GetById(weaponData.tableId);
		if (table == null || string.IsNullOrEmpty(table.prefab)) return;

		GameObject prefab = Resources.Load<GameObject>(table.prefab);
		if (prefab == null) return;

		_weaponInstance = Instantiate(prefab, _weaponBone);
		_weaponInstance.transform.localPosition = new Vector3(-0.229f, 0.654f, 0.402f);
		_weaponInstance.transform.localRotation = Quaternion.identity;
	}

	/// <summary>
	/// 장비창 클론용 레이어 재설정.
	/// </summary>
	public void SetWeaponLayer(int layer)
	{
		if (_weaponInstance == null) return;
		SetLayerRecursive(_weaponInstance, layer);
	}

	private static Transform FindBoneRecursive(Transform parent, string name)
	{
		if (parent.name == name) return parent;
		foreach (Transform child in parent)
		{
			Transform found = FindBoneRecursive(child, name);
			if (found != null) return found;
		}
		return null;
	}

	private static void SetLayerRecursive(GameObject obj, int layer)
	{
		obj.layer = layer;
		foreach (Transform child in obj.transform)
			SetLayerRecursive(child.gameObject, layer);
	}
}
