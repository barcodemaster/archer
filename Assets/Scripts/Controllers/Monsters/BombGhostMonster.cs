using System.Collections;
using UnityEngine;
using static Define;

/// <summary>
/// 홈 포지션 주변을 순회하다가 플레이어가 사거리 내에 들어오면
/// Arc 형태의 폭탄을 투척하는 유령 몬스터.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class BombGhostMonster : MonsterBase
{
	[Header("Wander")]
	[SerializeField] private float _wanderRadius = 4f;
	[SerializeField] private float _wanderSpeed = 1.5f;

	[Header("Attack")]
	[SerializeField] private float _attackRange = 10f;
	[SerializeField] private float _attackCooldown = 3f;
	[SerializeField] private GameObject _bombPrefab;

	[Header("Throw Animation")]
	[SerializeField] private float _throwPrepareTime = 0.5f;

	private Vector3 _homePosition;
	private Vector3 _wanderTarget;
	private bool _hasWanderTarget;
	private float _cooldownTimer;
	private bool _isThrowing;

	private void Awake()
	{
		_overlapWithPlayer = true;
	}

	protected override void Start()
	{
		base.Start();
		_homePosition = transform.position;
		_cooldownTimer = _attackCooldown;
	}

	protected override void Update()
	{
		base.Update();
		if (State == EState.Die || _isThrowing) return;

		_cooldownTimer -= Time.deltaTime;

		if (Target != null && _cooldownTimer <= 0f)
		{
			float dist = Vector3.Distance(transform.position, Target.position);
			if (dist <= _attackRange)
			{
				StartCoroutine(ThrowBombRoutine());
				return;
			}
		}

		State = EState.Move;
		UpdateWander();
	}

	/// <summary>
	/// 플레이어 방향으로 회전 후 폭탄을 투척한다.
	/// </summary>
	private IEnumerator ThrowBombRoutine()
	{
		_isThrowing = true;
		State = EState.Attack;
		FaceTarget();

		yield return new WaitForSeconds(_throwPrepareTime);

		if (State != EState.Die && Target != null && _bombPrefab != null)
		{
			Vector3 dir = (Target.position - transform.position).normalized;
			dir.y = 0;

			GameObject bomb = ObjectPool.Instance.Get(_bombPrefab);
			bomb.transform.position = transform.position + Vector3.up * 0.5f;
			bomb.transform.rotation = Quaternion.LookRotation(dir);
			bomb.GetComponent<ProjectileBase>()?.Init(Damage, Target.position);
		}

		_cooldownTimer = _attackCooldown;
		_isThrowing = false;
		State = EState.Idle;
	}

	/// <summary>
	/// 홈 포인트 주변을 천천히 걸어서 순회한다.
	/// </summary>
	private void UpdateWander()
	{
		if (!_hasWanderTarget || Vector3.Distance(transform.position, _wanderTarget) < 0.3f)
			PickWanderTarget();

		Vector3 dir = _wanderTarget - transform.position;
		dir.y = 0;
		if (dir.sqrMagnitude < 0.001f) return;
		dir.Normalize();
		transform.rotation = Quaternion.LookRotation(dir);

		Vector3 nextPos = transform.position + dir * _wanderSpeed * Time.deltaTime;
		if (CanMoveTo(nextPos))
			transform.position = nextPos;
		else
			PickWanderTarget();
	}

	/// <summary>
	/// 홈 포인트 주변에서 랜덤 순회 목적지를 선택한다.
	/// </summary>
	private void PickWanderTarget()
	{
		float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
		float dist = Random.Range(0.5f, _wanderRadius);
		_wanderTarget = _homePosition + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
		_hasWanderTarget = true;
	}
}
