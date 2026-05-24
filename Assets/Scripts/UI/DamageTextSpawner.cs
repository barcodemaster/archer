using UnityEngine;

/// <summary>
/// DamageText 프리팹을 월드 좌표에 스폰하는 정적 헬퍼.
/// </summary>
public static class DamageTextSpawner
{
	private static GameObject _prefab;

	/// <summary>
	/// 지정한 위치에 데미지 텍스트를 생성한다.
	/// </summary>
	public static void Spawn(Vector3 worldPos, float damage, bool isPlayer = false)
	{
		if (_prefab == null)
			_prefab = Resources.Load<GameObject>("Prefabs/DamageText");
		if (_prefab == null) return;

		GameObject go = ObjectPool.Instance.Get(_prefab);
		go.transform.position = worldPos;
		go.transform.rotation = Quaternion.identity;
		DamageText dt = go.GetComponent<DamageText>();
		if (dt != null)
			dt.Init(damage, isPlayer);
	}
}
