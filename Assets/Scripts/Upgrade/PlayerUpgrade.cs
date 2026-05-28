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

	private UpgradeInfo Info(EUpgradeType type) => UpgradeDatabase.GetInfo(type);

	// 공격속도: 1 + lv * (attackSpeed / 100)
	public float AttackSpeedMultiplier
	{
		get { var i = Info(EUpgradeType.AttackSpeed); return 1f + _attackSpeedLevel * (i?.attackSpeed ?? 15f) / 100f; }
	}

	public int FrontArrowLevel => GetLevel(EUpgradeType.FrontArrow);
	public int MultiShotLevel => GetLevel(EUpgradeType.MultiShot);
	public float MultiShotDelay { get { var i = Info(EUpgradeType.MultiShot); return i?.multiShotDelay ?? 0.15f; } }
	public bool IsPiercing => GetLevel(EUpgradeType.Piercing) > 0;

	// 헤드샷: lv * (probability / 100)
	public float HeadshotChance { get { var i = Info(EUpgradeType.Headshot); return _headshotLevel * (i?.probability ?? 8f) / 100f; } }

	public bool HasDiagonalArrow => GetLevel(EUpgradeType.DiagonalArrow) > 0;
	public bool IsWallBounce => GetLevel(EUpgradeType.WallBounce) > 0;
	public bool HasSideArrow => GetLevel(EUpgradeType.SideArrow) > 0;
	public int RicochetCount => GetLevel(EUpgradeType.Ricochet);
	public float RicochetRadius { get { var i = Info(EUpgradeType.Ricochet); return i?.ricochetRadius ?? 5f; } }
	public bool HasBackArrow => GetLevel(EUpgradeType.BackArrow) > 0;

	// 크리티컬: cm.critRate/100 * lv + dwarf.critRate/100
	public float CritChance
	{
		get
		{
			var cm = Info(EUpgradeType.CriticalMaster);
			var dw = Info(EUpgradeType.Dwarf);
			return _criticalMasterLevel * (cm?.critRate ?? 5f) / 100f
				+ (_dwarfLevel > 0 ? (dw?.critRate ?? 5f) / 100f : 0f);
		}
	}

	// 크리티컬 최소 데미지: critDamageBase/100 + lv * critDamage/100 + dwarf.critDamage/100
	public float CritDamageMin
	{
		get
		{
			var cm = Info(EUpgradeType.CriticalMaster);
			var dw = Info(EUpgradeType.Dwarf);
			return (cm?.critDamageBase ?? 150f) / 100f
				+ _criticalMasterLevel * (cm?.critDamage ?? 10f) / 100f
				+ (_dwarfLevel > 0 ? (dw?.critDamage ?? 10f) / 100f : 0f);
		}
	}

	// 크리티컬 최대 데미지: CritDamageMin + critDamageRange/100
	public float CritDamageMax
	{
		get { var cm = Info(EUpgradeType.CriticalMaster); return CritDamageMin + (cm?.critDamageRange ?? 10f) / 100f; }
	}

	// 공격 부스트: 1 + lv * (attackPercent / 100)
	public float AttackBoostMultiplier
	{
		get { var i = Info(EUpgradeType.AttackBoost); return 1f + _attackBoostLevel * (i?.attackPercent ?? 15f) / 100f; }
	}

	// 거인 공격력: 1 + (attackPercent / 100)
	public float GiantDamageMultiplier
	{
		get { var i = Info(EUpgradeType.Giant); return _giantLevel > 0 ? 1f + (i?.attackPercent ?? 10f) / 100f : 1f; }
	}

	public bool HasFastGrowth => _fastGrowthLevel > 0;
	public int MaxPlayerLevel { get { var i = Info(EUpgradeType.FastGrowth); return _fastGrowthLevel > 0 ? (int)(i?.maxLevelCap ?? 13f) : 10; } }
	public float HpBoostPercent { get { var i = Info(EUpgradeType.HpBoost); return (i?.maxHp ?? 20f) / 100f; } }
	public float HpBoostHealPercent { get { var i = Info(EUpgradeType.HpBoost); return (i?.healAmount ?? 50f) / 100f; } }

	public bool HasRage => _rageLevel > 0;
	public float RageAttackPerHpPercent { get { var i = Info(EUpgradeType.Rage); return i?.attackPerHpPercent ?? 0.5f; } }

	public bool HasExtraLife => _extraLifeLevel > 0 && !_extraLifeUsed;

	// 슬로우: 1 - lv * (enemySlowPercent / 100)
	public float SlowProjectileMultiplier
	{
		get { var i = Info(EUpgradeType.SlowProjectile); return 1f - _slowProjectileLevel * (i?.enemySlowPercent ?? 15f) / 100f; }
	}

	// 피의 갈증: lv * healAmount% (최대 HP 비율)
	public float BloodThirstHealPercent { get { var i = Info(EUpgradeType.BloodThirst); return _bloodThirstLevel * (i?.healAmount ?? 3f) / 100f; } }

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
