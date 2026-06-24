using UnityEngine;

/// <summary>
/// Attack 상태: 타겟에게 발사체를 발사하고 쿨다운 후 다음 행동을 결정한다.
/// </summary>
public class PetAttackState : IPetState
{
	public static readonly PetAttackState Instance = new();

	public void Enter(PetController pet)
	{
		pet.AttackTimer = 0f;
		pet.FaceDirection(pet.ChaseTarget.transform.position);
		pet.FireProjectile();
		pet.PlayAnimation(Define.ATTACK);
	}

	public void Update(PetController pet)
	{
		var cfg = GameConfig.Instance;
		pet.AttackTimer += Time.deltaTime;

		if (pet.AttackTimer < pet.AttackCooldown) return;

		// Return 체크
		if (pet.DistanceToPlayer > cfg.petReturnThreshold)
		{
			pet.ChaseTarget = null;
			pet.TransitionTo(PetReturnState.Instance);
			return;
		}

		// 타겟 사망
		if (pet.ChaseTarget == null || pet.ChaseTarget.IsDead)
		{
			pet.ChaseTarget = null;
			pet.TransitionTo(PetPatrolState.Instance);
			return;
		}

		// 사거리 내 → 재공격
		float dist = Vector3.Distance(pet.transform.position, pet.ChaseTarget.transform.position);
		if (dist <= cfg.petAttackRange)
		{
			pet.AttackTimer = 0f;
			pet.FaceDirection(pet.ChaseTarget.transform.position);
			pet.FireProjectile();
			pet.PlayAnimation(Define.ATTACK);
			return;
		}

		// 사거리 밖 → Chase
		pet.TransitionTo(PetChaseState.Instance);
	}

	public void Exit(PetController pet) { }
}
