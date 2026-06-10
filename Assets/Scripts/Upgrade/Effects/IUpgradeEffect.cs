using static Define;

/// <summary>
/// 업그레이드 효과 인터페이스. Command Pattern을 적용하여
/// 각 업그레이드 타입별 효과를 독립적인 클래스로 분리한다.
/// </summary>
public interface IUpgradeEffect
{
	EUpgradeType Type { get; }
	void Apply(PlayerController player, PlayerUpgrade upgrade);
}
