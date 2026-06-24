using UnityEngine;
using static Define;

public class SkeletonMonster : MonsterBase
{
	[SerializeField] private float _moveSpeed = 3f;

	protected override void Update()
	{
		base.Update();
		if (State == EState.Die || Target == null) return;

		Vector3 dir = GetPathDirection();
		if (dir == Vector3.zero) return;

		dir.y = 0;
		if (dir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(dir);

		Vector3 nextPos = transform.position + dir * _moveSpeed * Time.deltaTime;
		if (!CanMoveTo(nextPos))
		{
			StopMovement();
			return;
		}

		State = EState.Move;
		MoveToward(dir, _moveSpeed);
	}
}
