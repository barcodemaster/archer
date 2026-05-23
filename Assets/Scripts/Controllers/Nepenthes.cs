using System.Collections;
using UnityEngine;
using static Define;

public class Nepenthes : MonsterBase
{
	[SerializeField] private float _attackCooldown = 3f;
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private float _scatterRadius = 1.5f;
	[SerializeField] private int _projectileCount = 3;
	[SerializeField] private float _projectileInterval = 0.15f;

	private float _attackTimer;

	protected override void Update()
	{
		base.Update();
		if (State == EState.Die || Target == null) return;

		LookAtTarget();

		_attackTimer -= Time.deltaTime;
		if (_attackTimer <= 0f)
		{
			_attackTimer = _attackCooldown;
			State = EState.Attack;
			StartCoroutine(FireProjectiles());
		}
		else if (State != EState.Attack)
		{
			State = EState.Idle;
		}
	}

	private void LookAtTarget()
	{
		Vector3 dir = (Target.position - transform.position).normalized;
		dir.y = 0;
		if (dir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(dir);
	}

	private IEnumerator FireProjectiles()
	{
		for (int i = 0; i < _projectileCount; i++)
		{
			Vector2 randomOffset = Random.insideUnitCircle * _scatterRadius;
			Vector3 targetPos = Target.position + new Vector3(randomOffset.x, 0, randomOffset.y);
			SpawnProjectile(targetPos);
			if (i < _projectileCount - 1)
				yield return new WaitForSeconds(_projectileInterval);
		}
		if (State != EState.Die)
			State = EState.Idle;
	}

	private void SpawnProjectile(Vector3 targetPos)
	{
		if (_projectilePrefab == null) return;

		Vector3 spawnPos = transform.position + Vector3.up;
		GameObject go = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
		NepenthesProjectile proj = go.GetComponent<NepenthesProjectile>();
		if (proj != null)
			proj.Init(Damage, targetPos);
	}
}
