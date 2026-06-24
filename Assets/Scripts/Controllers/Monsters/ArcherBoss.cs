using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[RequireComponent(typeof(TilePassability))]
public class ArcherBoss : MonsterBase
{
    [Header("Attack")]
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private float _warningDuration = 1f;
    [SerializeField] private float _fireDelay = 0.4f;
	[SerializeField] private float _jumpRadius = 4f;
	[SerializeField] private int _maxAttackCount = 4;
	[SerializeField] private int _maxBounceCount = 4;
	[SerializeField] private float _projectileSpeed = 50f;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private LineRenderer _lineRenderer;

    private Material _lineMaterial;
    private float _attackTimer;
    private bool _isAttacking = false;
	private bool _isJumping = false;
    private int _attackCount;
	private Vector3 _nextPos;

	protected override void Start()
	{
        base.Start();
        _attackTimer = _attackCooldown * 0.5f;
        SetupLineRenderer();
		_attackCount = Random.Range(1, _maxAttackCount+1);
	}

    private void SetupLineRenderer()
    {
		if (_lineRenderer == null) return;

		_lineRenderer.startWidth = 0.08f;
		_lineRenderer.endWidth = 0.08f;
		_lineMaterial = new Material(Shader.Find("Sprites/Default"));
		_lineRenderer.material = _lineMaterial;
		_lineRenderer.startColor = Color.red;
		_lineRenderer.endColor = Color.red;
		_lineRenderer.numCornerVertices = 5;  // 코너를 둥글게 보간 → 굵기 일정
		_lineRenderer.numCapVertices = 2;     // 끝점도 둥글게
		_lineRenderer.positionCount = 0;
		_lineRenderer.enabled = false;
	}

	protected override void Update()
	{
		base.Update();

		if(State == Define.EState.Die) return;
		if (_isAttacking) return;
		if (Target == null) return;

		FaceTarget();

		if (_attackCount > 0)
		{
			_attackTimer -= Time.deltaTime;
			if(_attackTimer <= 0f && !_isAttacking)
			{
				StartCoroutine(AttackSequence());
				return;
			}
		}
		else if(!_isAttacking && !_isJumping)
		{
			StartCoroutine(JumpMove());
			return;
		}
	}

	private IEnumerator AttackSequence()
	{
		_isAttacking = true;
		StopMovement();
		State = EState.Attack;

		if (_lineRenderer != null)
			_lineRenderer.enabled = true;

		float elapsed = 0f;
		Vector3 aimDir = Vector3.forward;

		while(elapsed < _warningDuration)
		{
			if(State == EState.Die)
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

			Vector3[] path = CalculateBouncePath(origin, aimDir);

			if (_lineRenderer != null)
			{
				_lineRenderer.positionCount = path.Length;
				_lineRenderer.SetPositions(path);
			}

			yield return null;

		}

		// LineRenderer ��Ȱ��ȭ
		if (_lineRenderer != null)
		{
			_lineRenderer.enabled = false;
			_lineRenderer.positionCount = 0;
		}

		if (State == EState.Die)
			yield break;

		// ��� �� �߻� ������
		float delayElapsed = 0f;
		while (delayElapsed < _fireDelay)
		{
			if (State == EState.Die) yield break;
			delayElapsed += Time.deltaTime;
			yield return null;
		}

		if (State == EState.Die)
			yield break;

		// ���� �������� ȭ�� �߻�
		Vector3 fireOrigin = new Vector3(transform.position.x, 1f, transform.position.z);
		FireArrow(aimDir, fireOrigin);

		_attackTimer = _attackCooldown;
		_isAttacking = false;
		State = EState.Idle;
		_attackCount--;

	}

	private Vector3[] CalculateBouncePath(Vector3 origin, Vector3 direction)
	{
		List<Vector3> points = new List<Vector3> { origin };
		Vector3 pos = origin;
		Vector3 dir = direction;
		int maxBounces = _maxBounceCount;

		while(maxBounces-- > 0)
		{
			RaycastHit[] hits = Physics.RaycastAll(pos + dir * 0.05f, dir);

			float closestDist = float.MaxValue;
			RaycastHit closestHit = default;
			bool found = false;

			foreach (RaycastHit h in hits)
			{
				if (h.collider.GetComponent<BlockObstacle>() != null && h.distance < closestDist)
				{
					closestDist = h.distance;
					closestHit = h;
					found = true;
				}
			}

			if (!found) break;

			points.Add(closestHit.point);
			Vector3 normal = closestHit.normal;
			normal.y = 0;
			normal.Normalize();
			dir = Vector3.Reflect(dir, normal);
			dir.y = 0;
			dir.Normalize();
			pos = closestHit.point;
		}

		return points.ToArray();
	}

	private IEnumerator JumpMove()
	{
		_isJumping = true;

		Vector3 startPos = transform.position;

		Vector3 jumpDir = Random.insideUnitCircle.normalized;
		float distance = Random.Range(1f, _jumpRadius);

		Vector3 targetPos = startPos + new Vector3(jumpDir.x, 0f, jumpDir.y) * distance;
		if(!CanMoveTo(targetPos))
		{
			bool found = false;
			for (int i = 0; i < 10; i++)
			{
				float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
				jumpDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				Vector3 candidate = startPos + new Vector3(
					jumpDir.x * distance, 0f, jumpDir.z * distance
				);
				if (CanMoveTo(candidate))
				{
					targetPos = candidate;
					found = true;
					break;
				}
			}
			if(!found)
			{
				_isJumping = false;
				_attackCount = Random.Range(1, _maxAttackCount + 1);
				yield break;
			}
		}

		State = EState.Jump;
		yield return null; // 애니메이터 상태 전환 대기

		float duration = Anim.GetCurrentAnimatorStateInfo(0).length;
		float elapsed = 0f;

		while(elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / duration;
			transform.position = Vector3.Lerp(startPos, targetPos, t);

			yield return null;
		}

		transform.position = targetPos;
		_isJumping = false;
		_attackCount = Random.Range(1, _maxAttackCount+1);
	}

	private void FireArrow(Vector3 direction, Vector3 spawnPos)
	{
		if (_projectilePrefab == null) return;

		GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
		go.transform.position = spawnPos;
		go.transform.rotation = Quaternion.LookRotation(direction);
		ProjectileBase spawnedProjectile = go.GetComponent<ProjectileBase>();
		spawnedProjectile.Init(Damage, _maxBounceCount, _projectileSpeed, 100);
	}

	private void OnDisable()
	{
		CleanupLineRenderer();
		if (_lineMaterial != null)
		{
			Destroy(_lineMaterial);
			_lineMaterial = null;
		}
	}

	private void CleanupLineRenderer()
	{
		if (_lineRenderer != null)
		{
			_lineRenderer.enabled = false;
			_lineRenderer.positionCount = 0;
		}
	}

	protected override void OnDie()
	{
		CleanupLineRenderer();
	}
}
