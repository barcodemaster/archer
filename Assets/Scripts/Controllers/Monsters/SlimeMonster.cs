using System.Collections;
using UnityEngine;
using static Define;

public class SlimeMonster : MonsterBase
{
	[SerializeField] private GameObject _projectilePrefab;
	[SerializeField] private GameObject _childPrefab;
	[SerializeField] private float _attackCooldown;
	[SerializeField] private float _speed;
	[SerializeField] private float _childSpeed;
	[SerializeField] private float _arcDuration;
	[SerializeField] private float _arcHeight;
	[SerializeField] private float _patrolRadius;
	[SerializeField] private float _patroldelay;

	public bool _isChild = false;

	protected override void Start()
	{
		base.Start();
		StartCoroutine(BehaviourRoutine());
	}

	protected override void Update()
	{
		base.Update();
	}

	private IEnumerator BehaviourRoutine()
	{
		float curSpeed = _isChild ? _childSpeed : _speed;

		while (State != EState.Die)
		{
			// Patrol (single pass: move to random point, wait, return)
			yield return StartCoroutine(Patrol(_patrolRadius, _patroldelay, curSpeed));

			if (State == EState.Die) yield break;

			// Child는 Patrol만 반복
			if (_isChild)
				continue;

			// 부모: 공격 쿨다운 대기
			yield return new WaitForSeconds(_attackCooldown);
			if (State == EState.Die) yield break;

			// Arc 발사체 생성
			State = EState.Attack;
			ProjectileBase proj = ProjectileFactory.CreateArc(
				_projectilePrefab, transform.position, Target.position, Damage, _arcDuration);

			// 착탄 대기
			yield return new WaitForSeconds(_arcDuration);
			if (State == EState.Die) yield break;

			// Child 생성 — 발사체 착탄 지점에 배치
			Vector3 spawnPos = proj != null ? proj.TargetPos : transform.position;
			GameObject go = ObjectPool.Instance.Get(_childPrefab);
			go.transform.position = spawnPos;
			SlimeMonster childSlime = go.GetComponent<SlimeMonster>();
			childSlime._isChild = true;
			StageManager.Instance.RegisterMonster(childSlime);

			State = EState.Move;
		}
	}
}
