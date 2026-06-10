using UnityEngine;
using static Define;

/// <summary>
/// 몬스터 Attack 상태: 공격 애니메이션 재생.
/// 실제 공격 로직은 서브클래스에서 처리한다.
/// </summary>
public class MonsterAttackState : IMonsterState
{
	public static readonly MonsterAttackState Instance = new();

	public void Enter(MonsterBase monster)
	{
		monster.State = EState.Attack;
		monster.StopMovementPublic();
	}

	public void Update(MonsterBase monster) { }

	public void Exit(MonsterBase monster) { }
}
