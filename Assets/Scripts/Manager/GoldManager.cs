using System;
using UnityEngine;

/// <summary>
/// 골드를 관리하는 싱글톤 매니저.
/// </summary>
public class GoldManager : Singleton<GoldManager>
{
	private int _gold;
	public int Gold => _gold;
	public event Action OnGoldChanged;

	/// <summary>
	/// 골드를 추가한다.
	/// </summary>
	public void AddGold(int amount)
	{
		_gold += amount;
		AchievementManager.Instance.AddProgress(Define.EAchievementType.GoldEarned, amount);
		OnGoldChanged?.Invoke();
		SaveManager.Save();
	}

	/// <summary>
	/// 골드를 소비한다. 부족하면 false를 반환한다.
	/// </summary>
	public bool SpendGold(int amount)
	{
		if (_gold < amount) return false;
		_gold -= amount;
		OnGoldChanged?.Invoke();
		SaveManager.Save();
		return true;
	}

	/// <summary>
	/// 골드를 직접 설정한다 (로드용).
	/// </summary>
	public void SetGold(int amount)
	{
		_gold = amount;
		OnGoldChanged?.Invoke();
	}
}
