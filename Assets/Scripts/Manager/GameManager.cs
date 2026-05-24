using Unity.VisualScripting;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	public Vector2 JoystickDir { get; set; } = Vector2.zero;
	public bool IsPaused { get; set; } = false;
}
