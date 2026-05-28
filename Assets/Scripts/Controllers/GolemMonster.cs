using System.Collections;
using UnityEngine;
using static Define;

/// <summary>
/// 골렘 몬스터: 스폰 위치 기준으로 배회하다가 주기적으로 360° 스핀 후 3방향 투사체를 발사한다.
/// Walk 타입으로 타일맵 이동 제한을 받는다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class GolemMonster : MonsterBase
{
	[SerializeField] private float _moveSpeed = 1.5f;
	[SerializeField] private float _patrolRadius = 4f;
	[SerializeField] private float _attackCooldownMin = 3f;
	[SerializeField] private float _attackCooldownMax = 7f;
	[SerializeField] private float _spinDuration = 1f;
	[SerializeField] private GameObject _projectilePrefab;

	private Vector3 _wanderCenter;
	private Vector3 _wanderTarget;
	private float _attackTimer;
	private bool _isAttacking;

	protected override void Start()
	{
		base.Start();
		_wanderCenter = transform.position;
		_wanderTarget = GetRandomWanderTarget();
		_attackTimer = Random.Range(_attackCooldownMin, _attackCooldownMax);
	}

	protected override void Update()
	{
		base.Update();

		if (State == EState.Die) return;
		if (_isAttacking) return;

		_attackTimer -= Time.deltaTime;
		if (_attackTimer <= 0f)
		{
			StartCoroutine(AttackSequence());
			return;
		}

		Wander();
	}

	/// <summary>
	/// 스폰 위치 기준 반경 내 랜덤 지점으로 배회한다.
	/// </summary>
	private void Wander()
	{
		State = EState.Move;

		Vector3 toTarget = _wanderTarget - transform.position;
		toTarget.y = 0;

		if (toTarget.magnitude < 1f)
		{
			_wanderTarget = GetRandomWanderTarget();
			State = EState.Idle;
			return;
		}

		Vector3 dir = toTarget.normalized;
		Vector3 nextPos = transform.position + dir * _moveSpeed * Time.deltaTime;

		if (!CanMoveTo(nextPos))
		{
			_wanderTarget = GetRandomWanderTarget();
			return;
		}

		MoveToward(dir, _moveSpeed);

		if (dir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(dir);
	}

	/// <summary>
	/// 스폰 위치 기준 반경 내 랜덤 위치를 반환한다. 최대 15회 시도해 통과 가능한 경로를 선택한다.
	/// </summary>
	private Vector3 GetRandomWanderTarget()
	{
		for (int attempt = 0; attempt < 15; attempt++)
		{
			float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float dist = Random.Range(0f, _patrolRadius);
			Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
			Vector3 candidate = _wanderCenter + offset;
			if (IsWanderPathClear(transform.position, candidate))
				return candidate;
		}
		return _wanderCenter;
	}

	/// <summary>
	/// 목적지 및 경로 중간 3지점이 모두 통과 가능한지 확인한다.
	/// </summary>
	private bool IsWanderPathClear(Vector3 from, Vector3 to)
	{
		if (!CanMoveTo(to)) return false;
		for (int i = 1; i <= 3; i++)
		{
			Vector3 mid = Vector3.Lerp(from, to, i / 4f);
			if (!CanMoveTo(mid)) return false;
		}
		return true;
	}

	/// <summary>
	/// transform과 Rigidbody 양쪽을 플레이어 방향으로 정렬한다.
	/// </summary>
	private void FacePlayer()
	{
		if (Target == null) return;
		Vector3 toPlayer = Target.position - transform.position;
		toPlayer.y = 0;
		if (toPlayer.sqrMagnitude < 0.001f) return;
		Quaternion rot = Quaternion.LookRotation(toPlayer.normalized);
		transform.rotation = rot;
		if (Rb != null) Rb.rotation = rot;
	}

	/// <summary>
	/// 공격 시퀀스: 플레이어 방향 조준 → 360° 스핀 → 3방향 발사.
	/// </summary>
	private IEnumerator AttackSequence()
	{
		_isAttacking = true;
		State = EState.Attack;
		StopMovement();

		// 플레이어 방향 즉시 조준
		FacePlayer();

		// 360° 스핀
		float elapsed = 0f;
		while (elapsed < _spinDuration)
		{
			float rotThisFrame = (360f / _spinDuration) * Time.deltaTime;
			transform.Rotate(0f, rotThisFrame, 0f, Space.World);
			elapsed += Time.deltaTime;
			yield return null;
		}

		// 플레이어 방향 재정렬
		FacePlayer();

		FireThreeShots();

		_attackTimer = Random.Range(_attackCooldownMin, _attackCooldownMax);
		_isAttacking = false;
		State = EState.Move;
	}

	/// <summary>
	/// forward, +45°, -45° 방향으로 MonsterProjectile을 발사한다.
	/// </summary>
	private void FireThreeShots()
	{
		if (_projectilePrefab == null) return;

		float[] angles = { 0f, 45f, -45f };
		Vector3 baseDir = transform.forward;
		baseDir.y = 0;
		if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;
		else baseDir.Normalize();
		AudioManager.Instance?.PlayMonsterProjectile();
		foreach (float angle in angles)
		{
			Vector3 dir = Quaternion.Euler(0, angle, 0) * baseDir;
			Vector3 spawnPos = new Vector3(transform.position.x, 1f, transform.position.z);
			GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
			go.transform.position = spawnPos;
			go.transform.rotation = Quaternion.LookRotation(dir);
			go.GetComponent<ProjectileBase>()?.Init(Damage);
		}
	}
}
