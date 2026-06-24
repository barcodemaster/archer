using UnityEngine;

/// <summary>
/// Chase 상태: 타겟 몬스터를 추적하여 공격 사거리까지 접근한다.
/// </summary>
public class PetChaseState : IPetState
{
	public static readonly PetChaseState Instance = new();

	public void Enter(PetController pet)
	{
		pet.StateTimer = 0f;
		pet.PlayAnimation(Define.MOVE);
	}

	public void Update(PetController pet)
	{
		var cfg = GameConfig.Instance;
		pet.StateTimer += Time.deltaTime;

		// 타겟 유효성 체크
		if (pet.ChaseTarget == null || pet.ChaseTarget.IsDead)
		{
			pet.ChaseTarget = null;
			pet.TransitionTo(PetPatrolState.Instance);
			return;
		}

		// Return 체크
		if (pet.DistanceToPlayer > cfg.petReturnThreshold)
		{
			pet.ChaseTarget = null;
			pet.TransitionTo(PetReturnState.Instance);
			return;
		}

		// Chase 타임아웃
		if (pet.StateTimer > cfg.petChaseTimeout)
		{
			pet.ChaseTarget = null;
			pet.TransitionTo(PetPatrolState.Instance);
			return;
		}

		// 사거리 도달
		float dist = Vector3.Distance(pet.transform.position, pet.ChaseTarget.transform.position);
		if (dist <= cfg.petAttackRange)
		{
			pet.TransitionTo(PetAttackState.Instance);
			return;
		}

		// 추적 이동
		pet.MoveTo(pet.ChaseTarget.transform.position, cfg.petChaseSpeed);
	}

	public void Exit(PetController pet) { }
}
