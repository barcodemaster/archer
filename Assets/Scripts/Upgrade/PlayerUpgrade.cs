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

	[Header("Stat")]
	[SerializeField] private int _criticalMasterLevel;
	[SerializeField] private int _attackBoostLevel;
	[SerializeField] private int _fastGrowthLevel;
	[SerializeField] private int _hpBoostLevel;
	[SerializeField] private int _rageLevel;
	[SerializeField] private int _slowProjectileLevel;
	[SerializeField] private int _bloodThirstLevel;

	[Header("Passive")]
	[SerializeField] private int _wallPassLevel;
	[SerializeField] private int _waterWalkerLevel;
	[SerializeField] private int _dwarfLevel;
	[SerializeField] private int _giantLevel;
	[SerializeField] private int _extraLifeLevel;
	private bool _extraLifeUsed;

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
			EUpgradeType.CriticalMaster => _criticalMasterLevel,
			EUpgradeType.AttackBoost => _attackBoostLevel,
			EUpgradeType.FastGrowth => _fastGrowthLevel,
			EUpgradeType.HpBoost => _hpBoostLevel,
			EUpgradeType.WallPass => _wallPassLevel,
			EUpgradeType.WaterWalker => _waterWalkerLevel,
			EUpgradeType.Dwarf => _dwarfLevel,
			EUpgradeType.Giant => _giantLevel,
			EUpgradeType.Rage => _rageLevel,
			EUpgradeType.ExtraLife => _extraLifeLevel,
			EUpgradeType.SlowProjectile => _slowProjectileLevel,
			EUpgradeType.BloodThirst => _bloodThirstLevel,
			_ => 0,
		};
	}

	public void AddUpgrade(EUpgradeType type)
	{
		UpgradeInfo info = UpgradeDatabase.GetInfo(type);
		int maxLevel = info != null ? info.maxLevel : 1;
		if (GetLevel(type) >= maxLevel) return;

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
			case EUpgradeType.CriticalMaster: _criticalMasterLevel++; break;
			case EUpgradeType.AttackBoost: _attackBoostLevel++; break;
			case EUpgradeType.FastGrowth: _fastGrowthLevel++; break;
			case EUpgradeType.HpBoost: _hpBoostLevel++; break;
			case EUpgradeType.WallPass: _wallPassLevel++; break;
			case EUpgradeType.WaterWalker: _waterWalkerLevel++; break;
			case EUpgradeType.Dwarf: _dwarfLevel++; break;
			case EUpgradeType.Giant: _giantLevel++; break;
			case EUpgradeType.Rage: _rageLevel++; break;
			case EUpgradeType.ExtraLife: _extraLifeLevel++; break;
			case EUpgradeType.SlowProjectile: _slowProjectileLevel++; break;
			case EUpgradeType.BloodThirst: _bloodThirstLevel++; break;
		}
	}

	// 기존 능력 프로퍼티
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

	// 신규 능력 프로퍼티
	public float CritChance => _criticalMasterLevel * 0.05f + (_dwarfLevel > 0 ? 0.05f : 0f);
	public float CritDamageMin => 1.5f + _criticalMasterLevel * 0.1f + (_dwarfLevel > 0 ? 0.1f : 0f);
	public float CritDamageMax => CritDamageMin + 0.1f;
	public float AttackBoostMultiplier => 1f + _attackBoostLevel * 0.15f;
	public float GiantDamageMultiplier => _giantLevel > 0 ? 1.1f : 1f;
	public bool HasFastGrowth => _fastGrowthLevel > 0;
	public int MaxPlayerLevel => _fastGrowthLevel > 0 ? 13 : 10;
	public float HpBoostAmount => 200f;
	public bool HasRage => _rageLevel > 0;
	public bool HasExtraLife => _extraLifeLevel > 0 && !_extraLifeUsed;
	public float SlowProjectileMultiplier => 1f - _slowProjectileLevel * 0.15f;
	public float BloodThirstHeal => _bloodThirstLevel * 30f;

	public void ConsumeExtraLife() => _extraLifeUsed = true;

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (!Application.isPlaying) return;
		var player = GetComponent<PlayerController>();
		if (player == null) return;
		if (_wallPassLevel > 0) player.ApplyWallPassCollisions();
		if (_waterWalkerLevel > 0) player.ApplyWaterWalkCollisions();
	}
#endif
}
