using UnityEngine;
using static Define;

/// <summary>
/// 몬스터 Idle 상태: 대기 중. 타겟이 감지되면 Chase로 전환한다.
/// </summary>
public class MonsterIdleState : IMonsterState
{
	public static readonly MonsterIdleState Instance = new();

	public void Enter(MonsterBase monster)
	{
		monster.State = EState.Idle;
		monster.StopMovementPublic();
	}

	public void Update(MonsterBase monster)
	{
		if (monster.HasTarget)
			monster.TransitionTo(MonsterChaseState.Instance);
	}

	public void Exit(MonsterBase monster) { }
}
