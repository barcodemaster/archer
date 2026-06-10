using System.Collections.Generic;
using static Define;

/// <summary>
/// 업그레이드 효과를 등록하고 실행하는 레지스트리.
/// Command Pattern으로 switch문을 제거하고 확장성을 확보한다.
/// </summary>
public static class UpgradeEffectRegistry
{
	private static readonly Dictionary<EUpgradeType, IUpgradeEffect> _effects = new();
	private static bool _initialized;

	/// <summary>
	/// 모든 업그레이드 효과를 등록한다.
	/// </summary>
	private static void Init()
	{
		if (_initialized) return;
		_initialized = true;

		Register(new WallPassEffect());
		Register(new WaterWalkerEffect());
		Register(new DwarfEffect());
		Register(new GiantEffect());
		Register(new HpBoostEffect());
		Register(new FastGrowthEffect());
	}

	private static void Register(IUpgradeEffect effect)
	{
		_effects[effect.Type] = effect;
	}

	/// <summary>
	/// 해당 타입의 업그레이드 효과를 적용한다. 등록되지 않은 타입은 무시.
	/// </summary>
	public static void Apply(EUpgradeType type, PlayerController player, PlayerUpgrade upgrade)
	{
		Init();
		if (_effects.TryGetValue(type, out IUpgradeEffect effect))
			effect.Apply(player, upgrade);
	}
}

/// <summary>
/// WallPass: 장애물 통과 플래그를 추가하고 콜라이더를 트리거로 전환한다.
/// </summary>
public class WallPassEffect : IUpgradeEffect
{
	public EUpgradeType Type => EUpgradeType.WallPass;
	public void Apply(PlayerController player, PlayerUpgrade upgrade)
	{
		TilePassability passability = player.GetComponent<TilePassability>();
		if (passability != null)
			passability.AddFlag(ETilePassFlag.WallPass);
		player.ApplyWallPassCollisions();
	}
}

/// <summary>
/// WaterWalker: 수면 통과 플래그를 추가하고 콜라이더를 트리거로 전환한다.
/// </summary>
public class WaterWalkerEffect : IUpgradeEffect
{
	public EUpgradeType Type => EUpgradeType.WaterWalker;
	public void Apply(PlayerController player, PlayerUpgrade upgrade)
	{
		TilePassability passability = player.GetComponent<TilePassability>();
		if (passability != null)
			passability.AddFlag(ETilePassFlag.WaterWalk);
		player.ApplyWaterWalkCollisions();
	}
}

/// <summary>
/// Dwarf: 플레이어 크기를 축소한다.
/// </summary>
public class DwarfEffect : IUpgradeEffect
{
	public EUpgradeType Type => EUpgradeType.Dwarf;
	public void Apply(PlayerController player, PlayerUpgrade upgrade)
	{
		var info = UpgradeDatabase.GetInfo(EUpgradeType.Dwarf);
		player.transform.localScale *= info?.scale ?? 0.9f;
	}
}

/// <summary>
/// Giant: 플레이어 크기를 확대한다.
/// </summary>
public class GiantEffect : IUpgradeEffect
{
	public EUpgradeType Type => EUpgradeType.Giant;
	public void Apply(PlayerController player, PlayerUpgrade upgrade)
	{
		var info = UpgradeDatabase.GetInfo(EUpgradeType.Giant);
		player.transform.localScale *= info?.scale ?? 1.1f;
	}
}

/// <summary>
/// HpBoost: 최대 HP를 증가시키고 일정량 즉시 회복한다.
/// </summary>
public class HpBoostEffect : IUpgradeEffect
{
	public EUpgradeType Type => EUpgradeType.HpBoost;
	public void Apply(PlayerController player, PlayerUpgrade upgrade)
	{
		player.BoostHp(upgrade.HpBoostPercent, upgrade.HpBoostHealPercent);
	}
}

/// <summary>
/// FastGrowth: 최대 레벨 상한을 올린다.
/// </summary>
public class FastGrowthEffect : IUpgradeEffect
{
	public EUpgradeType Type => EUpgradeType.FastGrowth;
	public void Apply(PlayerController player, PlayerUpgrade upgrade)
	{
		ExpManager.Instance.SetMaxLevel(upgrade.MaxPlayerLevel);
	}
}
