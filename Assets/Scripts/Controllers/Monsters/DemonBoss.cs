using System.Collections;
using UnityEngine;
using static Define;

/// <summary>
/// 데몬 보스: Rush(돌진 + 양옆 Arc) / Fire(좌→우 스윕 Arc) 패턴을 반복한다.
/// 공격 쿨다운 동안 A* 경로로 플레이어를 추적한다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class DemonBoss : MonsterBase
{
	[Header("General")]
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private float _attackCooldown = 4f;
	[SerializeField] private float _speed = 3f;

	[Header("Rush")]
	[SerializeField] private float _rushDistance = 4f;
	[SerializeField] private float _rushSpeed = 20f;
	[SerializeField] private float _rushFinishDelay = 1.2f;
	[SerializeField] private int _maxRushCount = 3;

	[Header("Rush Arc Projectiles")]
	[SerializeField] private GameObject _arcProjectilePrefab;
	[SerializeField] private float _arcSideDistance = 4f;
	[SerializeField] private float _rushArcHeight = 2f;
	[SerializeField] private float _rushArcDuration = 0.8f;

	[Header("Fire Arc")]
	[SerializeField] private int _projCount = 30;
	[SerializeField] private float _spreadWidth = 3f;
	[SerializeField] private float _minArcDuration = 2.5f;
	[SerializeField] private float _maxArcDuration = 3.0f;
	[SerializeField] private float _minArcHeight = 1.3f;
	[SerializeField] private float _maxArcHeight = 1.7f;
	[SerializeField] private float _landingRandomRadius = 1f;

	private Coroutine _chaseCoroutine;

	protected override void Start()
	{
		base.Start();
		StartCoroutine(BehaviourRoutine());
	}

	protected override void Update()
	{
		base.Update();
	}

	/// <summary>
	/// 메인 행동 루프: 추적 → 대기 → 공격 패턴 반복.
	/// </summary>
	private IEnumerator BehaviourRoutine()
	{
		while (State != EState.Die)
		{
			// Phase 1: A* 추적 이동 시작
			_chaseCoroutine = StartCoroutine(ChaseLoop());

			// Phase 2: 쿨다운 대기
			float elapsed = 0f;
			while (elapsed < _attackCooldown)
			{
				if (State == EState.Die) yield break;
				elapsed += Time.deltaTime;
				yield return null;
			}

			// Phase 3: 추적 중지 → 공격 준비
			StopChase();

			if (State == EState.Die) yield break;

			FaceTarget();

			// Phase 4: 랜덤 패턴 실행
			if (Random.Range(0, 2) == 0)
				yield return StartCoroutine(RushRoutine());
			else
				yield return StartCoroutine(FireRoutine());

			if (State == EState.Die) yield break;

			State = EState.Idle;
		}
	}

	/// <summary>
	/// A* 경로를 따라 플레이어를 추적한다. 외부에서 StopCoroutine으로 중단.
	/// </summary>
	private IEnumerator ChaseLoop()
	{
		while (true)
		{
			if (Target == null) { yield return null; continue; }

			Vector3 dir = GetPathDirection();
			if (dir.sqrMagnitude > 0.01f)
			{
				FaceDirection(dir);
				State = EState.Move;
				MoveToward(dir, _speed);
			}
			yield return null;
		}
	}

	/// <summary>
	/// 추적 코루틴을 중지하고 이동을 멈춘다.
	/// </summary>
	private void StopChase()
	{
		if (_chaseCoroutine != null)
		{
			StopCoroutine(_chaseCoroutine);
			_chaseCoroutine = null;
		}
		StopMovement();
	}

	/// <summary>
	/// Rush: 정해진 방향으로 돌진하며, 벽 충돌 시 즉시 중단한다.
	/// 시작 시 양옆 Arc 발사체를 생성한다.
	/// </summary>
	private IEnumerator RushRoutine()
	{
		if (Target == null)
			yield break;

		int rushCount = Random.Range(1, _maxRushCount + 1);

		// 방향은 최초 1회만 계산
		FaceTarget();
		Vector3 dir = Target.transform.position - transform.position;
		dir.y = 0f;
		dir.Normalize();

		while (rushCount-- > 0)
		{
			if (State == EState.Die)
				yield break;

			Vector3 startPos = transform.position;
			Vector3 endPos = startPos + dir * _rushDistance;

			// Rush 시작 시 양옆 Arc 발사체 생성
			SpawnRushArcProjectiles(dir, startPos, _rushDistance);

			State = EState.Idle;
			State = EState.Skill1;

			// 매 프레임 벽 체크하며 돌진
			while (Vector3.Distance(transform.position, endPos) > 0.1f)
			{
				if (State == EState.Die) yield break;

				Vector3 nextPos = Vector3.MoveTowards(
					transform.position, endPos, _rushSpeed * Time.deltaTime);

				if (!CanMoveTo(nextPos))
					break;

				transform.position = nextPos;
				yield return null;
			}

			yield return new WaitForSeconds(_rushFinishDelay);
		}
	}

	/// <summary>
	/// Rush 경로의 시작/중간/끝 3지점에서 좌우로 Arc 발사체를 생성한다. (3×2 = 6개)
	/// </summary>
	private void SpawnRushArcProjectiles(Vector3 rushDir, Vector3 startPos, float rushDist)
	{
		GameObject prefab = _arcProjectilePrefab != null ? _arcProjectilePrefab : _projectilePrefab;
		if (prefab == null) return;

		Vector3 rightDir = Vector3.Cross(rushDir, Vector3.up).normalized;
		float[] ratios = { 0f, 0.5f, 1f };

		foreach (float ratio in ratios)
		{
			Vector3 spawnPos = startPos + rushDir * (rushDist * ratio) + Vector3.up * 0.5f;

			foreach (float side in new[] { 1f, -1f })
			{
				Vector3 targetPos = spawnPos + rightDir * side * _arcSideDistance;

				GameObject go = ObjectPool.Instance.Get(prefab);
				go.transform.position = spawnPos;

				ProjectileBase proj = go.GetComponent<ProjectileBase>();
				if (proj != null)
					proj.Init(Damage, targetPos, _rushArcHeight, _rushArcDuration);
			}
		}
	}

	/// <summary>
	/// Fire: 좌→우 스윕 패턴으로 Arc 발사체를 "다다닥" rapid-fire 생성한다.
	/// </summary>
	private IEnumerator FireRoutine()
	{
		if (Target == null)
			yield break;

		State = EState.Skill2;

		Vector3 targetDir = Target.transform.position - transform.position;
		float targetDist = targetDir.magnitude * 1.3f;
		targetDir.y = 0f;
		targetDir.Normalize();

		Vector3 rightDir = Vector3.Cross(targetDir, Vector3.up).normalized;

		// 한 프레임에 전부 생성
		for (int i = 0; i < _projCount; i++)
		{
			float spreadT = (_projCount > 1) ? (float)i / (_projCount - 1) : 0.5f;
			float lateral = Mathf.Lerp(-_spreadWidth, _spreadWidth, spreadT);

			Vector3 spawnPos = transform.position
				+ targetDir * 0.5f
				+ rightDir * lateral
				+ Vector3.up * Random.Range(0.5f, 1f);

			// 착탄 위치: 넓은 랜덤 반경
			float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
			Vector3 randomOffset = new Vector3(
				Mathf.Cos(angle) * _landingRandomRadius,
				0f,
				Mathf.Sin(angle) * _landingRandomRadius);

			Vector3 targetPos = spawnPos + targetDir * targetDist + randomOffset;

			// 완전 랜덤 duration/height
			float duration = Random.Range(_minArcDuration, _maxArcDuration);
			float height = Random.Range(_minArcHeight, _maxArcHeight);

			GameObject go = ObjectPool.Instance.Get(_projectilePrefab);
			go.transform.position = spawnPos;

			ProjectileBase proj = go.GetComponent<ProjectileBase>();
			if (proj != null)
				proj.Init(Damage, targetPos, height, duration);
		}

		// 발사체가 날아가는 동안 대기 후 Idle 복귀
		yield return new WaitForSeconds(_maxArcDuration);
	}

	/// <summary>
	/// 지정 방향으로 즉시 회전한다.
	/// </summary>
	private void FaceDirection(Vector3 dir)
	{
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.01f) return;
		Quaternion rot = Quaternion.LookRotation(dir);
		transform.rotation = rot;
		if (Rb != null) Rb.rotation = rot;
	}
}
