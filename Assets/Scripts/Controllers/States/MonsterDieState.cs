using UnityEngine;
using static Define;

/// <summary>
/// 몬스터 Die 상태: 사망 처리. 다른 상태로 전환되지 않는다.
/// </summary>
public class MonsterDieState : IMonsterState
{
	public static readonly MonsterDieState Instance = new();

	public void Enter(MonsterBase monster)
	{
		monster.State = EState.Die;
		monster.StopMovementPublic();
	}

	public void Update(MonsterBase monster) { }

	public void Exit(MonsterBase monster) { }
}
