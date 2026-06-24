using static Define;

/// <summary>
/// CSV에서 로드되는 업적 테이블 행 데이터.
/// </summary>
public class AchievementTable
{
	public int id;
	public string name;
	public string description;
	public string icon;
	public EAchievementType type;
	public int target;
	public EAchievementReward rewardType;
	public int rewardAmount;
}
