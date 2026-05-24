using UnityEngine;

public class ExpManager : Singleton<ExpManager>
{
	private int _level = 1;
	private int _currentExp = 0;

	public int Level => _level;
	public int CurrentExp => _currentExp;
	public int ExpToNextLevel => 50 + (_level * 30);

	public System.Action<int> OnLevelUp;
	public System.Action<int, int> OnExpChanged;

	public void AddExp(int amount)
	{
		_currentExp += amount;
		while (_currentExp >= ExpToNextLevel)
		{
			_currentExp -= ExpToNextLevel;
			_level++;
			OnLevelUp?.Invoke(_level);
		}
		OnExpChanged?.Invoke(_currentExp, ExpToNextLevel);
	}
}
