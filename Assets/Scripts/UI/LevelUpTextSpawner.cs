using UnityEngine;

/// <summary>
/// LevelUpText 프리팹을 월드 좌표에 스폰하는 정적 헬퍼.
/// </summary>
public static class LevelUpTextSpawner
{
	private static GameObject _prefab;

	/// <summary>
	/// 지정한 위치에 레벨업 텍스트를 생성한다.
	/// </summary>
	public static void Spawn(Vector3 worldPos)
	{
		if (_prefab == null)
			_prefab = Resources.Load<GameObject>("Prefabs/LevelUpText");
		if (_prefab == null) return;

		GameObject go = ObjectPool.Instance.Get(_prefab);
		go.transform.position = worldPos;
		go.transform.rotation = Quaternion.identity;
		LevelUpText lt = go.GetComponent<LevelUpText>();
		if (lt != null)
			lt.Init();
	}
}
