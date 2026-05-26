using UnityEngine;

/// <summary>
/// 프로젝타일을 차단하는 장애물 마커 컴포넌트.
/// </summary>
public class BlockObstacle : MonoBehaviour
{
	/// <summary>
	/// 경계벽 여부. true이면 WallPass로도 통과 불가.
	/// </summary>
	public bool IsBoundary { get; set; }
}
