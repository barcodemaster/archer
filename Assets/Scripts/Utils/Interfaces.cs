using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 객체의 공통 인터페이스.
/// PlayerController, MonsterBase가 구현한다.
/// </summary>
public interface IDamageable
{
	float MaxHp { get; }
	float CurrentHp { get; }
	void TakeDamage(float damage);
	void Heal(float amount);
	bool IsDead { get; }
	Transform transform { get; }
}

/// <summary>
/// ObjectPool에서 관리되는 객체의 공통 인터페이스.
/// 풀에서 꺼낼 때 OnPoolGet, 반환할 때 OnPoolReturn이 호출된다.
/// </summary>
public interface IPoolable
{
	void OnPoolGet();
	void OnPoolReturn();
}
