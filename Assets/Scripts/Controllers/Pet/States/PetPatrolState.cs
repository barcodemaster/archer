using UnityEngine;

/// <summary>
/// Patrol 상태: offset 주변을 랜덤 순찰하며 적 감지 시 Chase로 전환한다.
/// </summary>
public class PetPatrolState : IPetState
{
	public static readonly PetPatrolState Instance = new();

	public void Enter(PetController pet)
	{
		pet.PlayAnimation(Define.IDLE);
		pet.HasPatrolTarget = false;
		pet.PatrolWaitTimer = 0f;
	}

	public void Update(PetController pet)
	{
		var cfg = GameConfig.Instance;

		// Return 체크
		if (pet.DistanceToPlayer > cfg.petReturnThreshold)
		{
			pet.TransitionTo(PetReturnState.Instance);
			return;
		}

		// 적 감지
		MonsterBase enemy = pet.FindNearestMonster(cfg.petDetectionRange);
		if (enemy != null)
		{
			pet.ChaseTarget = enemy;
			pet.TransitionTo(PetChaseState.Instance);
			return;
		}

		// 대기 중
		if (!pet.HasPatrolTarget)
		{
			pet.PatrolWaitTimer -= Time.deltaTime;
			if (pet.PatrolWaitTimer > 0f) return;

			// 새 순찰 지점 선택 (최대 5회 재시도, 통과 불가 위치 제외)
			Vector3 candidate = pet.OffsetPosition;
			bool found = false;
			for (int attempt = 0; attempt < 5; attempt++)
			{
				Vector2 rand = Random.insideUnitCircle * cfg.petPatrolRadius;
				candidate = pet.OffsetPosition + new Vector3(rand.x, 0f, rand.y);
				if (pet.CanMoveTo(candidate)) { found = true; break; }
			}
			pet.PatrolTarget = found ? candidate : pet.OffsetPosition;
			pet.HasPatrolTarget = true;
			pet.PlayAnimation(Define.MOVE);
		}

		// 이동
		float dist = Vector3.Distance(pet.transform.position, pet.PatrolTarget);
		if (dist < 0.15f)
		{
			pet.HasPatrolTarget = false;
			pet.PatrolWaitTimer = cfg.petPatrolWaitTime;
			pet.PlayAnimation(Define.IDLE);
			return;
		}

		pet.MoveTo(pet.PatrolTarget, cfg.petPatrolSpeed);
	}

	public void Exit(PetController pet) { }
}
