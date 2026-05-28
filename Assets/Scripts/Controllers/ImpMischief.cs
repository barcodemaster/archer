using System.Collections;
using UnityEngine;
using static Define;

/// <summary>
/// Imp Mischief 보스: Chase → Skill 순환. 3가지 스킬(Fireball, Headbutt, Roll)을 셔플 순서로 사용한다.
/// </summary>
[RequireComponent(typeof(TilePassability))]
public class ImpMischief : MonsterBase
{
	[Header("Chase")]
	[SerializeField] private float _chaseSpeed = 2.5f;
	[SerializeField] private float _chaseDurationMin = 2f;
	[SerializeField] private float _chaseDurationMax = 4f;

	[Header("Skill 1 - Fireball")]
	[SerializeField] private GameObject _fireballPrefab;
	[SerializeField] private float _fireballSpreadAngle = 20f;
	[SerializeField] private float _fireballWindup = 0.5f;

	[Header("Skill 2 - Headbutt")]
	[SerializeField] private float _headbuttSpeed = 15f;
	[SerializeField] private float _headbuttDistance = 5f;
	[SerializeField] private float _headbuttDamageMultiplier = 2f;
	[SerializeField] private float _headbuttWindup = 0.3f;
	[SerializeField] private float _headbuttReturnSpeed = 5f;

	[Header("Skill 3 - Roll")]
	[SerializeField] private float _rollSpeed = 18f;
	[SerializeField] private float _rollMaxDistance = 10f;
	[SerializeField] private float _rollDamageMultiplier = 1.5f;
	[SerializeField] private float _rollWindup = 0.2f;

	private EState[] _skillAnimNumber = {EState.Skill1, EState.Skill2, EState.Skill3};
	private int[] _skillOrder = { 0, 1, 2 };
	private int _skillIndex;
	private float _chaseTimer;
	private bool _isSkilling;

	protected override void Start()
	{
		base.Start();
		ShuffleSkills();
		_chaseTimer = Random.Range(_chaseDurationMin, _chaseDurationMax);
	}

	protected override void Update()
	{
		base.Update();

		if (State == EState.Die) return;
		if (_isSkilling) return;
		if (Target == null) return;

		_chaseTimer -= Time.deltaTime;
		if (_chaseTimer <= 0f)
		{
			StartCoroutine(ExecuteNextSkill());
			return;
		}

		// Chase
		State = EState.Move;
		RefreshPath();
		Vector3 dir = GetPathDirection();

		if (dir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(dir);

		MoveToward(dir, _chaseSpeed);
	}

	/// <summary>
	/// Fisher-Yates 셔플로 스킬 순서를 랜덤화한다.
	/// </summary>
	private void ShuffleSkills()
	{
		for (int i = _skillOrder.Length - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(_skillOrder[i], _skillOrder[j]) = (_skillOrder[j], _skillOrder[i]);
		}
		_skillIndex = 0;
	}

	/// <summary>
	/// 다음 스킬을 실행하고 인덱스를 전진시킨다. 3개 모두 사용 후 리셔플.
	/// </summary>
	private IEnumerator ExecuteNextSkill()
	{
		_isSkilling = true;
		StopMovement();

		int skill = _skillOrder[_skillIndex];
		State = _skillAnimNumber[skill];

		switch (skill)
		{
			case 0: yield return SkillFireball(); break;
			case 1: yield return SkillHeadbutt(); break;
			case 2: yield return SkillRoll(); break;
		}

		_skillIndex++;
		if (_skillIndex >= _skillOrder.Length)
			ShuffleSkills();

		_chaseTimer = Random.Range(_chaseDurationMin, _chaseDurationMax);
		_isSkilling = false;
		State = EState.Move;
	}

	#region Skill 1 - Fireball

	private IEnumerator SkillFireball()
	{
		FaceTarget();
		yield return new WaitForSeconds(_fireballWindup);

		if (_fireballPrefab == null) yield break;

		Vector3 baseDir = transform.forward;
		baseDir.y = 0;
		if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;
		else baseDir.Normalize();

		Vector3 spawnPos = transform.position + Vector3.up * 0.5f + transform.forward * 0.5f;
		float[] angles = { 0f, _fireballSpreadAngle, -_fireballSpreadAngle };

		Collider bossCollider = GetComponent<Collider>();

		AudioManager.Instance?.PlayMonsterProjectile();
		foreach (float angle in angles)
		{
			Vector3 dir = Quaternion.Euler(0, angle, 0) * baseDir;
			GameObject go = ObjectPool.Instance.Get(_fireballPrefab);
			go.transform.position = spawnPos;
			go.transform.rotation = Quaternion.LookRotation(dir);
			go.GetComponent<ProjectileBase>()?.Init(Damage);

			// 보스 자신과의 충돌 무시
			if (bossCollider != null)
			{
				Collider projCollider = go.GetComponent<Collider>();
				if (projCollider != null)
					Physics.IgnoreCollision(bossCollider, projCollider);
			}
		}
	}

	#endregion

	#region Skill 2 - Headbutt

	private IEnumerator SkillHeadbutt()
	{
		FaceTarget();
		Vector3 chargeDir = transform.forward;
		chargeDir.y = 0;
		chargeDir.Normalize();

		// Root motion 비활성화 — 위치는 고정, 애니메이션은 avatar만 이동
		Anim.applyRootMotion = false;

		yield return new WaitForSeconds(_headbuttWindup);

		float headbuttDamage = Damage * _headbuttDamageMultiplier;
		bool hasHit = false;

		// 전진 구간: 가상 판정 위치를 점진적으로 이동시키며 OverlapSphere 판정
		float chargeDuration = _headbuttDistance / _headbuttSpeed;
		float elapsed = 0f;

		while (elapsed < chargeDuration)
		{
			elapsed += Time.deltaTime;
			float traveled = Mathf.Min(elapsed * _headbuttSpeed, _headbuttDistance);
			Vector3 hitCheckPos = transform.position + chargeDir * traveled;

			if (!hasHit)
			{
				Collider[] hits = Physics.OverlapSphere(hitCheckPos, 0.8f);
				foreach (Collider hit in hits)
				{
					PlayerController player = hit.GetComponent<PlayerController>();
					if (player != null)
					{
						player.TakeDamage(headbuttDamage);
						hasHit = true;
						break;
					}
				}
			}

			yield return null;
		}

		// 후퇴 구간: 판정 없이 애니메이션 복귀 대기
		float returnDuration = _headbuttDistance / _headbuttReturnSpeed;
		yield return new WaitForSeconds(returnDuration);

		Anim.applyRootMotion = false;
	}

	#endregion

	#region Skill 3 - Roll

	private IEnumerator SkillRoll()
	{
		FaceTarget();
		Vector3 rollDir = transform.forward;
		rollDir.y = 0;
		rollDir.Normalize();

		// Root motion 비활성화, kinematic 전환 → transform 직접 이동
		Anim.applyRootMotion = false;
		bool wasKinematic = Rb != null && Rb.isKinematic;
		if (Rb != null) Rb.isKinematic = true;

		yield return new WaitForSeconds(_rollWindup);

		float traveled = 0f;
		bool hasHit = false;
		float rollDamage = Damage * _rollDamageMultiplier;

		while (traveled < _rollMaxDistance)
		{
			float step = _rollSpeed * Time.deltaTime;
			Vector3 nextPos = transform.position + rollDir * step;

			if (!CanMoveTo(nextPos))
				break;

			transform.position = nextPos;
			traveled += step;

			// 플레이어 감지
			if (!hasHit)
			{
				Collider[] hits = Physics.OverlapSphere(transform.position, 0.8f);
				foreach (Collider hit in hits)
				{
					PlayerController player = hit.GetComponent<PlayerController>();
					if (player != null)
					{
						player.TakeDamage(rollDamage);
						hasHit = true;
						break;
					}
				}
			}

			yield return null;
		}

		// kinematic / root motion 복원
		if (Rb != null) Rb.isKinematic = wasKinematic;
		Anim.applyRootMotion = false;

		yield return new WaitForSeconds(0.3f);
	}

	#endregion

	/// <summary>
	/// transform과 Rigidbody 양쪽을 플레이어 방향으로 정렬한다.
	/// </summary>
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
