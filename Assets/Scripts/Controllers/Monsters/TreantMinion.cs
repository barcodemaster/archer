using UnityEngine;

/// <summary>
/// 점프 후 착지 시 4방향(상하좌우) 투사체를 발사하는 트리언트 미니언.
/// </summary>
public class TreantMinion : TreantMinionBase
{
	private static readonly Vector3[] CARDINAL_DIRS =
	{
		Vector3.forward,
		Vector3.back,
		Vector3.right,
		Vector3.left,
	};

	protected override void FireProjectiles()
	{
		if (_projectilePrefab == null) return;
		Vector3 spawnPos = new Vector3(transform.position.x, 1f, transform.position.z);
		foreach (Vector3 dir in CARDINAL_DIRS)
		{
			GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
			go.transform.position = spawnPos;
			go.transform.rotation = Quaternion.LookRotation(dir);
			ProjectileBase proj = go.GetComponent<ProjectileBase>();
			if (proj != null)
				proj.Init(_projectileDamage);
		}
	}
}
