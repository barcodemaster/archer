using UnityEngine;

/// <summary>
/// 난이도 스케일링을 관리한다. SRP 원칙에 따라 StageManager에서 분리.
/// 스테이지 인덱스에 따른 HP/데미지 배율, 부활 비용 등을 계산한다.
/// </summary>
public class DifficultyManager : Singleton<DifficultyManager>
{
	/// <summary>
	/// 해당 스테이지 인덱스에 맞는 난이도 배율을 반환한다.
	/// </summary>
	public float GetDifficultyMultiplier(int stageIndex)
	{
		return 1f + stageIndex * GameConfig.Instance.difficultyPerStage;
	}

	/// <summary>
	/// 해당 스테이지의 부활 비용을 반환한다.
	/// </summary>
	public int GetAliveCost(int stageIndex)
	{
		return stageIndex * GameConfig.Instance.aliveCostPerStage + GameConfig.Instance.aliveCostBase;
	}

	/// <summary>
	/// 몬스터에 난이도 배율을 적용한다.
	/// </summary>
	public void ApplyToMonster(MonsterBase monster, int stageIndex)
	{
		float mult = GetDifficultyMultiplier(stageIndex);
		monster.ApplyDifficultyScale(mult, mult);
	}
}
