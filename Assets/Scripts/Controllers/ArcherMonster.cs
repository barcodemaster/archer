using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 궁수 몬스터: 사거리 내 플레이어를 감지하면 빨간 LineRenderer로 반사 경로를 미리 보여준 뒤 화살을 발사한다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class ArcherMonster : MonsterBase
{
	[Header("Attack")]
	[SerializeField] private float _attackRange = 10f;
	[SerializeField] private float _attackCooldown = 4f;
	[SerializeField] private float _warningDuration = 1.5f;
	[SerializeField] private float _maxArrowDistance = 15f;
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private LineRenderer _lineRenderer;

	private float _attackTimer;
	private bool _isAttacking;

	protected override void Start()
	{
		base.Start();
		_attackTimer = _attackCooldown * 0.5f;
		SetupLineRenderer();
	}

	/// <summary>
	/// LineRenderer 초기 설정: 빨간색 얇은 선, 기본 비활성화.
	/// </summary>
	private void SetupLineRenderer()
	{
		if (_lineRenderer == null) return;

		_lineRenderer.startWidth = 0.08f;
		_lineRenderer.endWidth = 0.08f;
		_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		_lineRenderer.startColor = Color.red;
		_lineRenderer.endColor = Color.red;
		_lineRenderer.positionCount = 0;
		_lineRenderer.enabled = false;
	}

	protected override void Update()
	{
		base.Update();

		if (State == EState.Die) return;
		if (_isAttacking) return;
		if (Target == null) return;

		FaceTarget();

		_attackTimer -= Time.deltaTime;

		float distToPlayer = Vector3.Distance(transform.position, Target.position);

		if (distToPlayer <= _attackRange && _attackTimer <= 0f)
		{
			StartCoroutine(AttackSequence());
			return;
		}

		if (State != EState.Idle)
		{
			StopMovement();
			State = EState.Idle;
		}
	}

	/// <summary>
	/// 공격 시퀀스: 조준(플레이어 추적) → 화살 발사.
	/// 조준 중에는 플레이어 방향을 계속 갱신하며 LineRenderer도 실시간 업데이트한다.
	/// </summary>
	private IEnumerator AttackSequence()
	{
		_isAttacking = true;
		StopMovement();
		State = EState.Attack;

		if (_lineRenderer != null)
			_lineRenderer.enabled = true;

		// 조준 시간 동안 플레이어를 추적하며 경로를 실시간 갱신
		float elapsed = 0f;
		Vector3 aimDir = Vector3.forward;

		while (elapsed < _warningDuration)
		{
			if (State == EState.Die)
			{
				CleanupLineRenderer();
				yield break;
			}

			elapsed += Time.deltaTime;

			FaceTarget();

			Vector3 origin = new Vector3(transform.position.x, 1f, transform.position.z);
			aimDir = (Target.position - transform.position).normalized;
			aimDir.y = 0;
			aimDir.Normalize();

			Vector3[] path = CalculateBouncePath(origin, aimDir, _maxArrowDistance);

			if (_lineRenderer != null)
			{
				_lineRenderer.positionCount = path.Length;
				_lineRenderer.SetPositions(path);
			}

			yield return null;
		}

		// LineRenderer 비활성화
		if (_lineRenderer != null)
		{
			_lineRenderer.enabled = false;
			_lineRenderer.positionCount = 0;
		}

		if (State == EState.Die)
			yield break;

		// 최종 방향으로 화살 발사
		Vector3 fireOrigin = new Vector3(transform.position.x, 1f, transform.position.z);
		FireArrow(aimDir, fireOrigin);

		_attackTimer = _attackCooldown;
		_isAttacking = false;
		State = EState.Idle;
	}

	/// <summary>
	/// 반사 경로를 계산한다. BlockObstacle에 부딪히면 반사하며 총 이동거리 소진까지 반복.
	/// </summary>
	private Vector3[] CalculateBouncePath(Vector3 origin, Vector3 direction, float remainDistance)
	{
		List<Vector3> points = new List<Vector3> { origin };
		Vector3 pos = origin;
		Vector3 dir = direction;
		float dist = remainDistance;
		int maxBounces = 10;

		while (dist > 0f && maxBounces-- > 0)
		{
			// RaycastAll로 모든 히트를 가져온 뒤 BlockObstacle만 필터링
			RaycastHit[] hits = Physics.RaycastAll(pos + dir * 0.05f, dir, dist);
			RaycastHit? wallHit = null;
			float closestDist = float.MaxValue;

			foreach (RaycastHit h in hits)
			{
				if (h.collider.GetComponent<BlockObstacle>() != null && h.distance < closestDist)
				{
					closestDist = h.distance;
					wallHit = h;
				}
			}

			if (wallHit.HasValue)
			{
				RaycastHit hit = wallHit.Value;
				points.Add(hit.point);
				dist -= hit.distance;
				Vector3 normal = hit.normal;
				normal.y = 0;
				normal.Normalize();
				dir = Vector3.Reflect(dir, normal);
				dir.y = 0;
				dir.Normalize();
				pos = hit.point;
				continue;
			}

			// 벽을 만나지 않으면 남은 거리만큼 직진
			points.Add(pos + dir * dist);
			dist = 0f;
		}

		return points.ToArray();
	}

	/// <summary>
	/// 화살 프리팹을 ObjectPool에서 가져와 발사한다.
	/// </summary>
	private void FireArrow(Vector3 direction, Vector3 spawnPos)
	{
		if (_projectilePrefab == null) return;

		AudioManager.Instance?.PlayMonsterProjectile();
		GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
		go.transform.position = spawnPos;
		go.transform.rotation = Quaternion.LookRotation(direction);
		go.GetComponent<ProjectileBase>()?.Init(Damage);
	}

	/// <summary>
	/// transform과 Rigidbody를 플레이어 방향으로 정렬한다.
	/// </summary>
	private void OnDisable()
	{
		CleanupLineRenderer();
	}

	private void CleanupLineRenderer()
	{
		if (_lineRenderer != null)
		{
			_lineRenderer.enabled = false;
			_lineRenderer.positionCount = 0;
		}
	}

	private void FaceTarget()
	{
		if (Target == null) return;
		Vector3 toPlayer = Target.position - transform.position;
		toPlayer.y = 0;
		if (toPlayer.sqrMagnitude < 0.001f) return;
		Quaternion rot = Quaternion.LookRotation(toPlayer.normalized);
		transform.rotation = rot;
		if (Rb != null) Rb.rotation = rot;
	}
}
