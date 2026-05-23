using UnityEngine;
using static Define;

public class TilePassability : MonoBehaviour
{
	[SerializeField] private ETilePassFlag _passFlags = ETilePassFlag.Walk;

	public ETilePassFlag PassFlags
	{
		get => _passFlags;
		set => _passFlags = value;
	}

	/// <summary>
	/// 통과 플래그를 추가한다.
	/// </summary>
	public void AddFlag(ETilePassFlag flag)
	{
		_passFlags |= flag;
	}

	/// <summary>
	/// 통과 플래그를 제거한다.
	/// </summary>
	public void RemoveFlag(ETilePassFlag flag)
	{
		_passFlags &= ~flag;
	}
}
