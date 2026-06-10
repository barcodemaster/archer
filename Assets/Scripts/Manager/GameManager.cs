using UnityEngine;
using UnityEngine.U2D;

public class GameManager : Singleton<GameManager>
{
	public Vector2 JoystickDir { get; set; } = Vector2.zero;
	public bool IsPaused { get; set; } = false;

	/// <summary>
	/// 모바일 첫 프레임 스터터 방지를 위해 주요 리소스를 미리 로드한다.
	/// </summary>
	protected override void Awake()
	{
		base.Awake();
		PreloadResources();
	}

	private void PreloadResources()
	{
		LoadResource<TextAsset>("Data/EquipmentData");
		LoadResource<TextAsset>("Data/UpgradeData");
		LoadResource<GameObject>("Prefabs/DamageText");
		LoadResource<GameObject>("Prefabs/LevelUpText");
		LoadResource<SpriteAtlas>("UI/Icon");
		LoadResource<SpriteAtlas>("UI/Common");
	}

	private void LoadResource<T>(string path) where T : Object
	{
		T resource = Resources.Load<T>(path);
		if (resource == null)
			Logger.LogWarning("GameManager", $"Failed to preload resource: {path}");
	}

	public void RevivePlayer()
	{
		UIManager.Instance.ShowJoystick();
		PlayerController player = PlayerController.Instance;
		if (player != null)
			player.Revive();
	}
}
