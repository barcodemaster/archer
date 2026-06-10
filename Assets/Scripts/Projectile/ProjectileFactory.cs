using UnityEngine;

/// <summary>
/// 투사체 생성을 담당하는 Factory 클래스.
/// 투사체 초기화 로직을 중앙화하여 PlayerController, MonsterBase에서의 중복을 제거한다.
/// </summary>
public static class ProjectileFactory
{
	/// <summary>
	/// ObjectPool에서 투사체를 가져와 위치/방향을 설정하고 데미지 데이터로 초기화한다.
	/// </summary>
	public static ProjectileBase Create(GameObject prefab, Vector3 position, Vector3 direction, ProjectileInitData data)
	{
		if (prefab == null) return null;

		GameObject go = ObjectPool.Instance.Get(prefab);
		go.transform.position = position;
		go.transform.rotation = Quaternion.LookRotation(direction);

		ProjectileBase proj = go.GetComponent<ProjectileBase>();
		if (proj != null)
			proj.Init(data);

		return proj;
	}

	/// <summary>
	/// 단순 데미지만으로 투사체를 생성한다. 몬스터 발사체용.
	/// </summary>
	public static ProjectileBase Create(GameObject prefab, Vector3 position, Vector3 direction, float damage)
	{
		if (prefab == null) return null;

		GameObject go = ObjectPool.Instance.Get(prefab);
		go.transform.position = position;
		go.transform.rotation = Quaternion.LookRotation(direction);

		ProjectileBase proj = go.GetComponent<ProjectileBase>();
		if (proj != null)
			proj.Init(damage);

		return proj;
	}

	/// <summary>
	/// Arc(포물선) 투사체를 생성한다.
	/// </summary>
	public static ProjectileBase CreateArc(GameObject prefab, Vector3 position, Vector3 targetPos, float damage)
	{
		if (prefab == null) return null;

		GameObject go = ObjectPool.Instance.Get(prefab);
		go.transform.position = position;

		ProjectileBase proj = go.GetComponent<ProjectileBase>();
		if (proj != null)
			proj.Init(damage, targetPos);

		return proj;
	}
}
