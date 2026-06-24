using UnityEngine;

public class ExpManager : Singleton<ExpManager>
{
	private int _level = 1;
	private int _currentExp = 0;
	private int _maxLevel = 10;
	private PlayerUpgrade _playerUpgrade;

	public int Level => _level;
	public int CurrentExp => _currentExp;
	public int ExpToNextLevel => GameConfig.Instance.baseExpRequired + (_level * GameConfig.Instance.expPerLevel);

	public System.Action<int> OnLevelUp;
	public System.Action<int, int> OnExpChanged;

	/// <summary>
	/// 최대 레벨을 설정한다. 기존 값보다 높은 경우에만 적용.
	/// </summary>
	public void SetMaxLevel(int level) => _maxLevel = Mathf.Max(_maxLevel, level);

	private void Start()
	{
		_playerUpgrade = PlayerController.Instance?.GetComponent<PlayerUpgrade>();
	}

	public void AddExp(int amount)
	{
		// 빠른 성장: 경험치 1.5배
		if (_playerUpgrade == null)
			_playerUpgrade = PlayerController.Instance?.GetComponent<PlayerUpgrade>();
		if (_playerUpgrade != null && _playerUpgrade.HasFastGrowth)
			amount = (int)(amount * GameConfig.Instance.fastGrowthMultiplier);

		_currentExp += amount;
		while (_currentExp >= ExpToNextLevel && _level < _maxLevel)
		{
			_currentExp -= ExpToNextLevel;
			_level++;
			AudioManager.Instance.PlayLevelUp();

			// 레벨업 시 최대체력의 30% 회복
			PlayerController player = PlayerController.Instance;
			if (player != null)
				player.Heal(player.MaxHp * 0.3f);

			AchievementManager.Instance.SetProgress(Define.EAchievementType.LevelReached, _level);
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
