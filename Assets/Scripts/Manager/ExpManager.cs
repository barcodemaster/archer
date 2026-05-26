using UnityEngine;

public class ExpManager : Singleton<ExpManager>
{
	private int _level = 1;
	private int _currentExp = 0;
	private int _maxLevel = 10;

	public int Level => _level;
	public int CurrentExp => _currentExp;
	public int ExpToNextLevel => 50 + (_level * 30);

	public System.Action<int> OnLevelUp;
	public System.Action<int, int> OnExpChanged;

	/// <summary>
	/// 최대 레벨을 설정한다. 기존 값보다 높은 경우에만 적용.
	/// </summary>
	public void SetMaxLevel(int level) => _maxLevel = Mathf.Max(_maxLevel, level);

	public void AddExp(int amount)
	{
		// 빠른 성장: 경험치 1.5배
		PlayerUpgrade upgrade = FindAnyObjectByType<PlayerUpgrade>();
		if (upgrade != null && upgrade.HasFastGrowth)
			amount = (int)(amount * 1.5f);

		_currentExp += amount;
		while (_currentExp >= ExpToNextLevel && _level < _maxLevel)
		{
			_currentExp -= ExpToNextLevel;
			_level++;
			OnLevelUp?.Invoke(_level);

			GameObject playerObj = GameObject.FindWithTag("Player");
			if (playerObj != null)
				LevelUpTextSpawner.Spawn(playerObj.transform.position + Vector3.up * 2f);
		}

		if (_level >= _maxLevel)
			_currentExp = 0;

		OnExpChanged?.Invoke(_currentExp, ExpToNextLevel);
	}
}
