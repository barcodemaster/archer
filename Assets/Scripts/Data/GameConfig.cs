using UnityEngine;

/// <summary>
/// 게임 전체 밸런스 수치를 중앙 관리하는 ScriptableObject.
/// 매직 넘버를 제거하고 Inspector에서 조정할 수 있도록 한다.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Data/GameConfig")]
public class GameConfig : ScriptableObject
{
	private static GameConfig _instance;
	public static GameConfig Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Resources.Load<GameConfig>("Data/GameConfig");
				if (_instance == null)
				{
					Logger.LogWarning("GameConfig", "GameConfig asset not found. Using defaults.");
					_instance = CreateInstance<GameConfig>();
				}
			}
			return _instance;
		}
	}

	[Header("Difficulty Scaling")]
	[Tooltip("스테이지당 난이도 증가 비율")]
	public float difficultyPerStage = 0.15f;

	[Header("Experience")]
	[Tooltip("기본 경험치 요구량")]
	public int baseExpRequired = 50;
	[Tooltip("레벨당 추가 경험치 요구량")]
	public int expPerLevel = 30;
	[Tooltip("빠른 성장 경험치 배율")]
	public float fastGrowthMultiplier = 1.5f;

	[Header("Drop Rates")]
	[Tooltip("HP 하트 드롭 확률")]
	public float hpHeartDropChance = 0.15f;
	[Tooltip("장비 드롭 확률")]
	public float equipmentDropChance = 0.05f;

	[Header("Gold")]
	[Tooltip("스테이지 클리어 보상 골드")]
	public int stageGoldReward = 100;
	[Tooltip("몬스터 골드 최소값")]
	public int monsterGoldMin = 5;
	[Tooltip("몬스터 골드 최대값")]
	public int monsterGoldMax = 15;

	[Header("Alive Cost")]
	[Tooltip("부활 비용 기본값")]
	public int aliveCostBase = 100;
	[Tooltip("스테이지당 추가 부활 비용")]
	public int aliveCostPerStage = 50;

	[Header("Monster AI")]
	[Tooltip("A* 경로 갱신 주기(초)")]
	public float pathRefreshInterval = 0.5f;
	[Tooltip("넉백 지속 시간(초)")]
	public float knockbackDuration = 0.15f;

	[Header("Player")]
	[Tooltip("발걸음 소리 간격(초)")]
	public float footstepInterval = 0.3f;

	[Header("Audio")]
	[Tooltip("BGM 페이드 인/아웃 시간(초)")]
	public float bgmFadeDuration = 1f;

	[Header("Closest Monster Cache")]
	[Tooltip("가장 가까운 몬스터 캐싱 주기(초)")]
	public float closestMonsterCacheInterval = 0.1f;
}
