using UnityEngine;
using static Define;

/// <summary>
/// 포물선 점프로 이동하는 유령 몬스터.
/// 일정 인터벌마다 플레이어 방향(chaseRange 내) 또는 랜덤 방향으로 점프한다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class GhostMonster : MonsterBase
{
	[SerializeField] private float _jumpIntervalMin = 1.5f;
	[SerializeField] private float _jumpIntervalMax = 3f;
	[SerializeField] private float _jumpDistance = 3f;
	[SerializeField] private float _jumpHeight = 2f;
	[SerializeField] private float _jumpDuration = 0.5f;
	[SerializeField] private float _chaseRange = 8f;
	[SerializeField] private float _wanderRadius = 4f;

	[SerializeField] private float _wanderSpeed = 1f;

	private Vector3 _homePosition;
	private float _jumpTimer;
	private bool _isJumping;
	private Vector3 _jumpStart;
	private Vector3 _jumpEnd;
	private float _jumpProgress;
	private Vector3 _wanderTarget;
	private bool _hasWanderTarget;

	private void Awake()
	{
		_overlapWithPlayer = true;
	}

	protected override void Start()
	{
		base.Start();
		_homePosition = transform.position;
		_jumpTimer = Random.Range(_jumpIntervalMin, _jumpIntervalMax);
	}

	protected override void Update()
	{
		base.Update();
		if (State == EState.Die) return;

		if (_isJumping)
			UpdateJump();
		else
		{
			_jumpTimer -= Time.deltaTime;
			if (_jumpTimer <= 0f)
			{
				StartJump();
				return;
			}
			State = EState.Move;
			UpdateWander();
		}
	}

	/// <summary>
	/// 포물선 궤적으로 점프 위치를 갱신한다.
	/// </summary>
	private void UpdateJump()
	{
		_jumpProgress += Time.deltaTime / _jumpDuration;
		if (_jumpProgress >= 1f)
		{
			_jumpProgress = 1f;
			_isJumping = false;
			transform.position = new Vector3(_jumpEnd.x, 0f, _jumpEnd.z);
			_jumpTimer = Random.Range(_jumpIntervalMin, _jumpIntervalMax);
			State = EState.Idle;
			return;
		}

		Vector3 pos = Vector3.Lerp(_jumpStart, _jumpEnd, _jumpProgress);
		pos.y = Mathf.Sin(_jumpProgress * Mathf.PI) * _jumpHeight;
		transform.position = pos;
	}

	/// <summary>
	/// 점프를 시작한다. 착지 지점이 막혀 있으면 랜덤 방향으로 재시도한다.
	/// </summary>
	private void StartJump()
	{
		_jumpStart = transform.position;
		Vector3 jumpDir = GetJumpDirection();
		_jumpEnd = new Vector3(
			_jumpStart.x + jumpDir.x * _jumpDistance,
			0f,
			_jumpStart.z + jumpDir.z * _jumpDistance
		);

		if (!CanMoveTo(_jumpEnd))
		{
			bool found = false;
			for (int i = 0; i < 10; i++)
			{
				float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
				jumpDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				Vector3 candidate = new Vector3(
					_jumpStart.x + jumpDir.x * _jumpDistance,
					0f,
					_jumpStart.z + jumpDir.z * _jumpDistance
				);
				if (CanMoveTo(candidate))
				{
					_jumpEnd = candidate;
					found = true;
					break;
				}
			}
			if (!found)
			{
				// 유효한 착지점 없음 → 점프 취소, 짧은 대기 후 재시도
				_jumpTimer = 0.5f;
				return;
			}
		}

		if (jumpDir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(jumpDir);

		_jumpProgress = 0f;
		_isJumping = true;
		State = EState.Move;
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

	/// <summary>
	/// chaseRange 내에 플레이어가 있으면 플레이어 방향, 아니면 홈 포인트 주변 랜덤 목적지 방향을 반환한다.
	/// </summary>
	private Vector3 GetJumpDirection()
	{
		if (Target != null && Vector3.Distance(transform.position, Target.position) <= _chaseRange)
		{
			Vector3 dir = Target.position - transform.position;
			dir.y = 0;
			return dir.normalized;
		}
		// 홈 포인트 주변 랜덤 목적지
		float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
		float dist = Random.Range(0f, _wanderRadius);
		Vector3 wanderTarget = _homePosition + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
		Vector3 toTarget = wanderTarget - transform.position;
		toTarget.y = 0;
		if (toTarget.sqrMagnitude < 0.01f)
			return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
		return toTarget.normalized;
	}
}
