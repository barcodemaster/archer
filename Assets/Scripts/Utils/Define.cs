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
		FrontArrow,		// 전방화살
		MultiShot,		// 멀티 샷
		Piercing,		// 관통 샷
		Headshot,		// 헤드 샷
		DiagonalArrow,	// 사선 화살
		WallBounce,		// 벽 반사
		SideArrow,		// 측면 화살
		Ricochet,		// 반동
		BackArrow,		// 후방 화살
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
}
