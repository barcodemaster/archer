using static Define;

/// <summary>
/// 드롭 시 생성되는 고유 아이템 인스턴스.
/// </summary>
[System.Serializable]
public class EquipmentData
{
	public string uid;
	public int tableId;
	public int level = 1;
	public ESubStatType subStatType;
	public float subStatValue;
	public bool isEquipped;
	public EEquipSlot equippedSlot;

	/// <summary>
	/// 현재 레벨의 메인 스탯을 계산한다.
	/// </summary>
	public float GetMainStat(EquipmentTable table)
	{
		return table.baseMainStat + (level - 1) * table.mainStatPerLevel;
	}

	/// <summary>
	/// 다음 레벨업에 필요한 골드를 계산한다.
	/// </summary>
	public int GetLevelUpCost(EquipmentTable table)
	{
		return table.goldCostBase + level * table.goldCostPerLevel;
	}
}
