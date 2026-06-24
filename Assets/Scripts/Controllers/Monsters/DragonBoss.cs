using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class DragonBoss : MonsterBase
{
	[SerializeField] private float _attackCooldown = 2f;
	[SerializeField] private float _attackAnimDuration = 0.5f;
	[SerializeField] private float _projectileSpeed = 8f;
	[SerializeField] private float _splitDelay = 1f;
	[SerializeField] private float _speed = 5f;
	[SerializeField] private float _offset = 1f;
	[SerializeField] private int _maxBounce = 2;
	[SerializeField] private float _projectileLifeTime = 5f;
	[SerializeField] private GameObject _projectilePrefab;

	private readonly List<Vector3> _dirs = new()
	{ Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

	private Coroutine _behaviorRoutine;

	protected override void Start()
	{
		base.Start();
		_behaviorRoutine = StartCoroutine(BehaviorLoop());
	}

	protected override void Update()
	{
		base.Update();
	}

	/// <summary>
	/// 단일 코루틴으로 Patrol → Attack → Split 흐름을 순차 제어한다.
	/// </summary>
	private IEnumerator BehaviorLoop()
	{
		while (State != EState.Die)
		{
			// Phase 1: Patrol (공격 쿨다운 동안 배회)
			Coroutine patrol = StartCoroutine(PatrolLoop());
			yield return new WaitForSeconds(_attackCooldown);
			StopCoroutine(patrol);

			if (State == EState.Die) yield break;

			// Phase 2: Attack (플레이어를 바라보고 발사체 생성)
			FaceTarget();
			State = EState.Attack;

			var (obj, proj) = SpawnProjectile();

			yield return new WaitForSeconds(_attackAnimDuration);

			if (State == EState.Die) yield break;

			// Phase 3: Split (fire-and-forget으로 분열 처리)
			if (proj != null)
				StartCoroutine(SplitSequence(obj, proj));

			State = EState.Idle;
		}
	}

	/// <summary>
	/// 발사체를 생성하고 플레이어 방향으로 초기화한다.
	/// </summary>
	private (GameObject, ProjectileBase) SpawnProjectile()
	{
		GameObject obj = ObjectPool.Instance.Get(_projectilePrefab);
		ProjectileBase proj = obj.GetComponent<ProjectileBase>();

		if (proj != null)
		{
			Vector3 dir = Target.position - transform.position;
			dir.y = 0f;
			proj.transform.position = transform.position
				+ transform.forward * _offset + transform.up * 1f;
			proj.transform.rotation = Quaternion.LookRotation(dir);
			proj.Init(Damage, _maxBounce, _projectileSpeed, _splitDelay + 1f);
		}

		return (obj, proj);
	}

	/// <summary>
	/// 원본 발사체 위치에서 4방향 분열 발사체를 생성한 뒤 원본을 반환한다.
	/// </summary>
	private IEnumerator SplitSequence(GameObject originalObj, ProjectileBase originalProj)
	{
		yield return new WaitForSeconds(
			Mathf.Max(0f, _splitDelay - _attackAnimDuration));

		foreach (var dir in _dirs)
		{
			GameObject splitObj = ObjectPool.Instance.Get(_projectilePrefab);
			ProjectileBase splitProj = splitObj.GetComponent<ProjectileBase>();

			if (splitProj != null)
			{
				splitProj.transform.localScale =
					_projectilePrefab.transform.localScale * 0.5f;
				splitProj.transform.position =
					originalProj.transform.position + dir * 1f;
				splitProj.transform.rotation = Quaternion.LookRotation(dir);
				splitProj.Init(Damage, _maxBounce, _projectileSpeed, _projectileLifeTime);
			}
		}

		ObjectPool.Instance.Return(originalObj);
	}

	/// <summary>
	/// Patrol 상태를 반복 실행한다. 외부에서 StopCoroutine으로 중단.
	/// </summary>
	private IEnumerator PatrolLoop()
	{
		while (true)
		{
			FaceTarget();
			yield return Patrol(10f, 1f, _speed);
		}
	}
}
