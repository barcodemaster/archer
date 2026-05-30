using UnityEngine;

public class Define
{
	public enum ETileType
	{
		Path  = 0,
		Wall  = 1,
		Water = 2,
		Spike = 3,
	}

	[System.Flags]
	public enum ETilePassFlag
	{
		None      = 0,
		Walk      = 1 << 0,
		Fly       = 1 << 1,
		WaterWalk = 1 << 2,
		WallPass  = 1 << 3,
	}

	public enum EProjectileMoveType
	{
		Straight,
		Arc,
		Piercing,
	}

	public enum EState
	{
		None,
		Idle,
		Move,
		Attack,
		Die,
		Jump,
		Skill1,
		Skill2,
		Skill3,
	}

	public enum EUpgradeType
	{
		AttackSpeed,	// 공격속도
		FrontArrow,		// 전방 화살
		MultiShot,		// 멀티 샷
		Piercing,		// 관통
		Headshot,		// 헤드 샷
		DiagonalArrow,	// 사선 화살
		WallBounce,		// 벽 반사
		SideArrow,		// 측면 화살
		Ricochet,		// 반동
		BackArrow,		// 후방 화살
		CriticalMaster,	// 크리티컬 마스터
		AttackBoost,	// 공격 부스트
		FastGrowth,		// 빠른 성장
		HpBoost,		// HP 부스트
		WallPass,		// 장애물 통과
		WaterWalker,	// 워터 워커
		Dwarf,			// 난쟁이
		Giant,			// 거인
		Rage,			// 분노
		ExtraLife,		// Extra Life
		SlowProjectile,	// 느린 발사체
		BloodThirst,	// 피의 갈증
	}

	public static int IDLE = Animator.StringToHash("Idle");
	public static int MOVE = Animator.StringToHash("Move");
	public static int ATTACK = Animator.StringToHash("Attack");
	public static int DIE = Animator.StringToHash("Die");
	public static int SERVING_IDLE = Animator.StringToHash("ServingIdle");
	public static int SERVING_MOVE = Animator.StringToHash("ServingMove");
	public static int JUMP = Animator.StringToHash("Jump");
	public static int SKILL1 = Animator.StringToHash("Skill1");
	public static int SKILL2 = Animator.StringToHash("Skill2");
	public static int SKILL3 = Animator.StringToHash("Skill3");

	public enum EEquipSlot { Weapon, Top, Bottom, Bracelet, Ring1, Ring2, Pet1, Pet2 }
	public enum EEquipGrade { Common, Uncommon, Rare, Epic, Legendary }
	public enum EMainStatType { Attack, MaxHP }
	public enum ESubStatType { AttackSpeed, CritChance, CritDamage, DamageResistance }

	/// <summary>
	/// 등급별 색상을 반환한다.
	/// </summary>
	public static Color GetGradeColor(EEquipGrade grade)
	{
		switch (grade)
		{
			case EEquipGrade.Common:    return Color.white;
			case EEquipGrade.Uncommon:  return new Color(0.3f, 0.85f, 0.3f);
			case EEquipGrade.Rare:      return new Color(0.3f, 0.5f, 1f);
			case EEquipGrade.Epic:      return new Color(0.7f, 0.3f, 0.9f);
			case EEquipGrade.Legendary: return new Color(1f, 0.8f, 0.2f);
			default:                    return Color.white;
		}
	}
}
