using UnityEngine;
using static Define;

public class PlayerUpgrade : MonoBehaviour
{
	[Header("Attack")]
	[SerializeField] private int _attackSpeedLevel;
	[SerializeField] private int _frontArrowLevel;
	[SerializeField] private int _multiShotLevel;

	[Header("Special")]
	[SerializeField] private int _piercingLevel;
	[SerializeField] private int _headshotLevel;
	[SerializeField] private int _wallBounceLevel;
	[SerializeField] private int _ricochetLevel;

	[Header("Direction")]
	[SerializeField] private int _diagonalArrowLevel;
	[SerializeField] private int _sideArrowLevel;
	[SerializeField] private int _backArrowLevel;

	public int GetLevel(EUpgradeType type)
	{
		return type switch
		{
			EUpgradeType.AttackSpeed => _attackSpeedLevel,
			EUpgradeType.FrontArrow => _frontArrowLevel,
			EUpgradeType.MultiShot => _multiShotLevel,
			EUpgradeType.Piercing => _piercingLevel,
			EUpgradeType.Headshot => _headshotLevel,
			EUpgradeType.WallBounce => _wallBounceLevel,
			EUpgradeType.Ricochet => _ricochetLevel,
			EUpgradeType.DiagonalArrow => _diagonalArrowLevel,
			EUpgradeType.SideArrow => _sideArrowLevel,
			EUpgradeType.BackArrow => _backArrowLevel,
			_ => 0,
		};
	}

	public void AddUpgrade(EUpgradeType type)
	{
		if (GetLevel(type) >= 1) return;

		switch (type)
		{
			case EUpgradeType.AttackSpeed: _attackSpeedLevel++; break;
			case EUpgradeType.FrontArrow: _frontArrowLevel++; break;
			case EUpgradeType.MultiShot: _multiShotLevel++; break;
			case EUpgradeType.Piercing: _piercingLevel++; break;
			case EUpgradeType.Headshot: _headshotLevel++; break;
			case EUpgradeType.WallBounce: _wallBounceLevel++; break;
			case EUpgradeType.Ricochet: _ricochetLevel++; break;
			case EUpgradeType.DiagonalArrow: _diagonalArrowLevel++; break;
			case EUpgradeType.SideArrow: _sideArrowLevel++; break;
			case EUpgradeType.BackArrow: _backArrowLevel++; break;
		}
	}

	public float AttackSpeedMultiplier => 1f + GetLevel(EUpgradeType.AttackSpeed) * 0.15f;
	public int FrontArrowLevel => GetLevel(EUpgradeType.FrontArrow);
	public int MultiShotLevel => GetLevel(EUpgradeType.MultiShot);
	public float MultiShotDelay => 0.15f;
	public bool IsPiercing => GetLevel(EUpgradeType.Piercing) > 0;
	public float HeadshotChance => GetLevel(EUpgradeType.Headshot) * 0.08f;
	public bool HasDiagonalArrow => GetLevel(EUpgradeType.DiagonalArrow) > 0;
	public bool IsWallBounce => GetLevel(EUpgradeType.WallBounce) > 0;
	public bool HasSideArrow => GetLevel(EUpgradeType.SideArrow) > 0;
	public int RicochetCount => GetLevel(EUpgradeType.Ricochet);
	public float RicochetRadius => 5f;
	public bool HasBackArrow => GetLevel(EUpgradeType.BackArrow) > 0;
}
