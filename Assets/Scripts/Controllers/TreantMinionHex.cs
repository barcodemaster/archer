using UnityEngine;

/// <summary>
/// 점프 후 착지 시 6방향(60도 간격) 투사체를 발사하는 트리언트 미니언.
/// </summary>
public class TreantMinionHex : TreantMinionBase
{
	private static readonly Vector3[] HEX_DIRS;

	static TreantMinionHex()
	{
		HEX_DIRS = new Vector3[6];
		for (int i = 0; i < 6; i++)
		{
			float angle = i * 60f * Mathf.Deg2Rad;
			HEX_DIRS[i] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
		}
	}

	protected override void FireProjectiles()
	{
		if (_projectilePrefab == null) return;
		AudioManager.Instance?.PlayMonsterProjectile();
		Vector3 spawnPos = new Vector3(transform.position.x, 1f, transform.position.z);
		foreach (Vector3 dir in HEX_DIRS)
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
