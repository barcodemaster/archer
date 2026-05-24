using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 4방향 점프 후 착지 시 4방향 투사체를 발사하는 트리언트 미니언 몬스터.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class TreantMinion : MonsterBase
{
	[SerializeField] private float _jumpIntervalMin = 2f;
	[SerializeField] private float _jumpIntervalMax = 4f;
	[SerializeField] private float _jumpDistance = 3f;
	[SerializeField] private float _jumpHeight = 1.5f;
	[SerializeField] private float _jumpDuration = 0.6f;
	[SerializeField] private float _attackDelay = 0.2f;
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private float _projectileDamage = 15f;

	private static readonly Vector3[] CARDINAL_DIRS =
	{
		Vector3.forward,
		Vector3.back,
		Vector3.right,
		Vector3.left,
	};

	private float _jumpTimer;
	private bool _isJumping;
	private Vector3 _jumpStart;
	private Vector3 _jumpEnd;
	private float _jumpProgress;

	private void Awake()
	{
		_overlapWithPlayer = true;
	}

	protected override void Start()
	{
		base.Start();
		_jumpTimer = Random.Range(_jumpIntervalMin, _jumpIntervalMax);
	}

	protected override void Update()
	{
		base.Update();
		if (State == EState.Die) return;

		if (_isJumping)
			UpdateJump();
		else if (State != EState.Attack)
		{
			_jumpTimer -= Time.deltaTime;
			if (_jumpTimer <= 0f)
				StartJump();
			else
				State = EState.Idle;
		}
	}

	/// <summary>
	/// 포물선 궤적으로 점프 위치를 갱신하고, 착지 시 4방향 발사를 시작한다.
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
			State = EState.Attack;
			StartCoroutine(FireAndReturn());
			return;
		}

		Vector3 pos = Vector3.Lerp(_jumpStart, _jumpEnd, _jumpProgress);
		pos.y = Mathf.Sin(_jumpProgress * Mathf.PI) * _jumpHeight;
		transform.position = pos;
	}

	/// <summary>
	/// 플레이어 방향 기준 최적 4방향으로 점프를 시작한다.
	/// </summary>
	private void StartJump()
	{
		_jumpStart = transform.position;
		Vector3 jumpDir = GetBestCardinalDirection();
		_jumpEnd = new Vector3(
			_jumpStart.x + jumpDir.x * _jumpDistance,
			0f,
			_jumpStart.z + jumpDir.z * _jumpDistance
		);

		if (!CanMoveTo(_jumpEnd))
		{
			bool found = false;
			foreach (Vector3 dir in GetOtherCardinalDirs(jumpDir))
			{
				Vector3 candidate = new Vector3(
					_jumpStart.x + dir.x * _jumpDistance,
					0f,
					_jumpStart.z + dir.z * _jumpDistance
				);
				if (CanMoveTo(candidate))
				{
					jumpDir = dir;
					_jumpEnd = candidate;
					found = true;
					break;
				}
			}
			if (!found)
			{
				_jumpTimer = 0.5f;
				return;
			}
		}

		transform.rotation = Quaternion.LookRotation(jumpDir);
		_jumpProgress = 0f;
		_isJumping = true;
		State = EState.Jump;
	}

	/// <summary>
	/// 플레이어 방향과 내적이 가장 큰 4방향 벡터를 반환한다.
	/// 플레이어 없으면 랜덤 4방향.
	/// </summary>
	private Vector3 GetBestCardinalDirection()
	{
		if (Target == null)
			return CARDINAL_DIRS[Random.Range(0, 4)];

		Vector3 toPlayer = Target.position - transform.position;
		toPlayer.y = 0;
		if (toPlayer.sqrMagnitude < 0.001f)
			return CARDINAL_DIRS[Random.Range(0, 4)];

		toPlayer.Normalize();
		float maxDot = float.MinValue;
		Vector3 best = Vector3.forward;
		foreach (Vector3 dir in CARDINAL_DIRS)
		{
			float dot = Vector3.Dot(toPlayer, dir);
			if (dot > maxDot) { maxDot = dot; best = dir; }
		}
		return best;
	}

	/// <summary>
	/// 선택된 방향을 제외한 나머지 3방향을 셔플해 반환한다.
	/// </summary>
	private Vector3[] GetOtherCardinalDirs(Vector3 chosen)
	{
		List<Vector3> others = new();
		foreach (Vector3 dir in CARDINAL_DIRS)
			if (dir != chosen) others.Add(dir);
		for (int i = others.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(others[i], others[j]) = (others[j], others[i]);
		}
		return others.ToArray();
	}

	private IEnumerator FireAndReturn()
	{
		yield return new WaitForSeconds(_attackDelay);
		FireFourWay();
		yield return new WaitForSeconds(0.3f);
		if (State != EState.Die)
			State = EState.Idle;
	}

	/// <summary>
	/// 4방향으로 MonsterProjectile을 동시에 발사한다.
	/// </summary>
	private void FireFourWay()
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
