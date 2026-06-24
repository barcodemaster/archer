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
	public float difficultyPerStage = 0.06f;

	[Header("Experience")]
	[Tooltip("기본 경험치 요구량")]
	public int baseExpRequired = 100;
	[Tooltip("레벨당 추가 경험치 요구량")]
	public int expPerLevel = 40;
	[Tooltip("빠른 성장 경험치 배율")]
	public float fastGrowthMultiplier = 1.5f;

	[Header("Drop Rates")]
	[Tooltip("HP 하트 드롭 확률")]
	public float hpHeartDropChance = 0.12f;
	[Tooltip("장비 드롭 확률")]
	public float equipmentDropChance = 0.05f;

	[Header("Gold")]
	[Tooltip("스테이지 클리어 보상 골드")]
	public int stageGoldReward = 150;
	[Tooltip("몬스터 골드 최소값")]
	public int monsterGoldMin = 5;
	[Tooltip("몬스터 골드 최대값")]
	public int monsterGoldMax = 20;

	[Header("Alive Cost")]
	[Tooltip("부활 비용 기본값")]
	public int aliveCostBase = 100;
	[Tooltip("스테이지당 추가 부활 비용")]
	public int aliveCostPerStage = 80;

	[Header("Monster AI")]
	[Tooltip("A* 경로 갱신 주기(초)")]
	public float pathRefreshInterval = 0.5f;
	[Tooltip("넉백 지속 시간(초)")]
	public float knockbackDuration = 0.15f;

	[Header("Hit Feedback")]
	[Tooltip("Squash 시 Y 스케일")]
	public float hitSquashScaleY = 0.8f;
	[Tooltip("Squash 시 XZ 스케일")]
	public float hitSquashScaleXZ = 1.2f;
	[Tooltip("Squash 지속 시간(초)")]
	public float hitSquashDuration = 0.05f;
	[Tooltip("Stretch 시 Y 스케일")]
	public float hitStretchScaleY = 1.1f;
	[Tooltip("Stretch 시 XZ 스케일")]
	public float hitStretchScaleXZ = 0.9f;
	[Tooltip("Stretch 지속 시간(초)")]
	public float hitStretchDuration = 0.05f;
	[Tooltip("원래 스케일로 복귀 시간(초)")]
	public float hitRecoverDuration = 0.1f;

	[Header("Player")]
	[Tooltip("발걸음 소리 간격(초)")]
	public float footstepInterval = 0.3f;

	[Header("Audio")]
	[Tooltip("BGM 페이드 인/아웃 시간(초)")]
	public float bgmFadeDuration = 1f;

	[Header("Closest Monster Cache")]
	[Tooltip("가장 가까운 몬스터 캐싱 주기(초)")]
	public float closestMonsterCacheInterval = 0.1f;

	[Header("Pet")]
	[Tooltip("펫1 플레이어 오프셋 (로컬 좌표)")]
	public Vector3 petOffset1 = new Vector3(-0.8f, 0f, -0.5f);
	[Tooltip("펫2 플레이어 오프셋 (로컬 좌표)")]
	public Vector3 petOffset2 = new Vector3(0.8f, 0f, -0.5f);
	[Tooltip("펫 공격 쿨다운(초)")]
	public float petAttackCooldown = 1.5f;
	[Tooltip("펫 스케일")]
	public float petScale = 0.5f;

	[Header("Pet AI")]
	[Tooltip("Patrol 랜덤 이동 반경")]
	public float petPatrolRadius = 1.0f;
	[Tooltip("Patrol 이동 속도")]
	public float petPatrolSpeed = 1.5f;
	[Tooltip("Patrol 도착 후 대기 시간(초)")]
	public float petPatrolWaitTime = 0.8f;
	[Tooltip("Return 트리거 거리")]
	public float petReturnThreshold = 6.0f;
	[Tooltip("Return 이동 속도")]
	public float petReturnSpeed = 5.0f;
	[Tooltip("Chase 이동 속도")]
	public float petChaseSpeed = 3.5f;
	[Tooltip("적 감지 범위")]
	public float petDetectionRange = 5.0f;
	[Tooltip("공격 사거리")]
	public float petAttackRange = 2.5f;
	[Tooltip("Chase 포기 시간(초)")]
	public float petChaseTimeout = 4.0f;
}
