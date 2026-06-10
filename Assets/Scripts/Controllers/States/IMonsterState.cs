/// <summary>
/// 몬스터 AI 상태 인터페이스. State Pattern을 적용하여 상태 전환 로직을 분리한다.
/// </summary>
public interface IMonsterState
{
	void Enter(MonsterBase monster);
	void Update(MonsterBase monster);
	void Exit(MonsterBase monster);
}
