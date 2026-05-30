using static Define;

/// <summary>
/// CSV에서 파싱되는 장비 기본 정보.
/// </summary>
public class EquipmentTable
{
	public int id;
	public string name;
	public string description;
	public string icon;
	public EEquipSlot slot;
	public EEquipGrade grade;
	public EMainStatType mainStatType;
	public float baseMainStat;
	public float mainStatPerLevel;
	public float baseSubMin;
	public float baseSubMax;
	public int goldCostBase;
	public int goldCostPerLevel;
	public int dropWeight;
	public string prefab;  // Resources 경로 (예: "Prefabs/PP_Theme_02_Bow_001")
}
