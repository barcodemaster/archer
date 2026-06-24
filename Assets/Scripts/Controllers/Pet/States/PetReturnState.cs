using UnityEngine;

/// <summary>
/// Return 상태: 플레이어 offset 위치로 복귀한다. 목적지는 진입 시 잠금된다.
/// </summary>
public class PetReturnState : IPetState
{
	public static readonly PetReturnState Instance = new();

	public void Enter(PetController pet)
	{
		pet.LockedReturnTarget = pet.OffsetPosition;
		pet.PlayAnimation(Define.MOVE);
	}

	public void Update(PetController pet)
	{
		Vector3 a = pet.transform.position;
		Vector3 b = pet.LockedReturnTarget;
		float dist = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
		if (dist < 0.2f)
		{
			pet.TransitionTo(PetPatrolState.Instance);
			return;
		}

		pet.MoveTo(pet.LockedReturnTarget, GameConfig.Instance.petReturnSpeed);
	}

	public void Exit(PetController pet)
	{
		pet.LockedReturnTarget = Vector3.zero;
	}
}
