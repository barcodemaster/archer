/// <summary>
/// 펫 AI 상태 인터페이스. IMonsterState 패턴을 따르되 PetController를 받는다.
/// </summary>
public interface IPetState
{
	void Enter(PetController pet);
	void Update(PetController pet);
	void Exit(PetController pet);
}
