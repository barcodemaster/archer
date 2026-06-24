using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 업적 진행도와 해금을 관리하는 싱글톤 매니저.
/// Observer 패턴으로 업적 해금 시 이벤트를 발행한다.
/// </summary>
public class AchievementManager : Singleton<AchievementManager>
{
	private Dictionary<EAchievementType, int> _progress = new();
	private HashSet<int> _unlocked = new();

	public event Action<AchievementTable> OnAchievementUnlocked;

	/// <summary>
	/// 누적형 진행도를 추가한다 (KillCount, GoldEarned, EquipmentCollected, BossKill, UpgradeSelected).
	/// </summary>
	public void AddProgress(EAchievementType type, int amount = 1)
	{
		if (!_progress.ContainsKey(type))
			_progress[type] = 0;
		_progress[type] += amount;
		CheckUnlocks(type);
	}

	/// <summary>
	/// 도달형 진행도를 설정한다 (StageClear, LevelReached). 기존 값보다 높을 때만 갱신.
	/// </summary>
	public void SetProgress(EAchievementType type, int value)
	{
		if (!_progress.ContainsKey(type))
			_progress[type] = 0;
		if (value > _progress[type])
		{
			_progress[type] = value;
			CheckUnlocks(type);
		}
	}

	/// <summary>
	/// 특정 타입의 현재 진행도를 반환한다.
	/// </summary>
	public int GetProgress(EAchievementType type)
	{
		return _progress.TryGetValue(type, out int val) ? val : 0;
	}

	/// <summary>
	/// 해당 업적이 해금되었는지 확인한다.
	/// </summary>
	public bool IsUnlocked(int id) => _unlocked.Contains(id);

	/// <summary>
	/// 세이브 데이터로 변환한다.
	/// </summary>
	public AchievementSaveData ToSaveData()
	{
		var data = new AchievementSaveData();
		foreach (var kvp in _progress)
			data.progress.Add(new AchievementProgressEntry { type = kvp.Key, value = kvp.Value });
		data.unlockedIds.AddRange(_unlocked);
		return data;
	}

	/// <summary>
	/// 세이브 데이터에서 복원한다.
	/// </summary>
	public void LoadFromSave(AchievementSaveData data)
	{
		_progress.Clear();
		_unlocked.Clear();

		if (data == null) return;

		foreach (var entry in data.progress)
			_progress[entry.type] = entry.value;
		foreach (int id in data.unlockedIds)
			_unlocked.Add(id);
	}

	/// <summary>
	/// 해당 타입의 모든 업적을 순회하며 해금 조건을 확인한다.
	/// </summary>
	private void CheckUnlocks(EAchievementType type)
	{
		int current = GetProgress(type);
		var achievements = AchievementDatabase.GetByType(type);

		for (int i = 0; i < achievements.Count; i++)
		{
			AchievementTable achievement = achievements[i];
			if (_unlocked.Contains(achievement.id)) continue;
			if (current >= achievement.target)
				Unlock(achievement);
		}
	}

	/// <summary>
	/// 업적을 해금하고 보상을 지급한다.
	/// </summary>
	private void Unlock(AchievementTable achievement)
	{
		_unlocked.Add(achievement.id);

		// 보상 지급
		switch (achievement.rewardType)
		{
			case EAchievementReward.Gold:
				GoldManager.Instance.AddGold(achievement.rewardAmount);
				break;
		}

		Logger.Log("AchievementManager", $"Achievement unlocked: {achievement.name} (+{achievement.rewardAmount} gold)");
		OnAchievementUnlocked?.Invoke(achievement);
	}
}
