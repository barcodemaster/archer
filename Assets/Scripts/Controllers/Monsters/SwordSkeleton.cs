using System.Collections;
using UnityEngine;
using static Define;

public class SwordSkeleton : MonsterBase
{
	[SerializeField] private float _speed = 5f;
	[SerializeField] private float _attackCooldown = 0.8f;
	[SerializeField] private float _attackRange = 1.8f;
	[SerializeField] private float _stabMoveDist = 1.5f;
	[SerializeField] private float _stabDuration = 0.5f;
	[SerializeField] private Collider _weaponCollider;

	private bool _isAttacking = false;

	protected override void Start()
	{
		base.Start();
		if (_weaponCollider != null)
			_weaponCollider.enabled = false;
	}

	protected override void Update()
	{
		base.Update();

		if (State == EState.Die || _isAttacking) return;

		Vector3 dir = GetPathDirection();
		if (dir == Vector3.zero) return;

		dir.y = 0;
		if (dir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(dir);

		Vector3 nextPos = transform.position + dir * _speed * Time.deltaTime;
		if (!CanMoveTo(nextPos))
		{
			StopMovement();
			return;
		}

		State = EState.Move;
		MoveToward(dir, _speed);

		if (Target != null && Vector3.Distance(Target.position, transform.position) <= _attackRange)
		{
			StopMovement();
			FaceTarget();

			if (Random.value < 0.5f)
				StartCoroutine(SwingRoutine());
			else
				StartCoroutine(StabRoutine());
		}
	}

	private IEnumerator SwingRoutine()
	{
		_isAttacking = true;
		if (_weaponCollider != null) _weaponCollider.enabled = true;
		State = EState.Skill1;

		// 1프레임 대기하여 CrossFade 전환 후 애니메이션 길이를 측정
		yield return null;

		float length = Anim.GetCurrentAnimatorStateInfo(0).length;
		yield return new WaitForSeconds(length);

		if (_weaponCollider != null) _weaponCollider.enabled = false;
		_isAttacking = false;
	}

	private IEnumerator StabRoutine()
	{
		_isAttacking = true;
		if (_weaponCollider != null) _weaponCollider.enabled = true;
		State = EState.Skill2;
		FaceTarget();

		Vector3 dir = Target.position - transform.position;
		dir.y = 0f;
		dir.Normalize();

		Vector3 startPos = transform.position;
		Vector3 targetPos = transform.position + dir * _stabMoveDist;

		float elapsed = 0f;

		while (elapsed < _stabDuration && State != EState.Die)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / _stabDuration;
			Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);

			if (CanMoveTo(nextPos))
				transform.position = nextPos;
			else
				break;

			yield return null;
		}

		yield return new WaitForSeconds(_attackCooldown);
		if (_weaponCollider != null) _weaponCollider.enabled = false;
		_isAttacking = false;
	}

	protected override void OnTriggerEnter(Collider other)
	{
		// 무기 콜라이더 히트: 공격 중일 때만 1회성 데미지
		if (_isAttacking && _weaponCollider != null && _weaponCollider.enabled)
		{
			PlayerController player = other.GetComponent<PlayerController>();
			if (player != null)
			{
				player.TakeDamage(Damage);
				_weaponCollider.enabled = false;
				return;
			}
		}

		base.OnTriggerEnter(other);
	}
}
