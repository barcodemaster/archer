using UnityEngine;
using static Define;

/// <summary>
/// 몬스터 Chase 상태: A* 경로를 따라 플레이어를 추적한다.
/// 서브클래스에서 오버라이드하여 커스텀 추적 로직을 적용할 수 있다.
/// </summary>
public class MonsterChaseState : IMonsterState
{
	public static readonly MonsterChaseState Instance = new();

	public void Enter(MonsterBase monster)
	{
		monster.State = EState.Move;
	}

	public void Update(MonsterBase monster)
	{
		if (!monster.HasTarget)
		{
			monster.TransitionTo(MonsterIdleState.Instance);
			return;
		}

		monster.State = EState.Move;
	}

	public void Exit(MonsterBase monster) { }
}
