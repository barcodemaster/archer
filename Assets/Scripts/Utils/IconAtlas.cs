using UnityEngine;
using UnityEngine.U2D;

[CreateAssetMenu(menuName = "Game/IconAtlas")]
public class IconAtlas : ScriptableObject
{
	[SerializeField] private SpriteAtlas _spriteAtlas;

	public Sprite GetSprite(string name)
	{
		if (_spriteAtlas == null) return null;
		return _spriteAtlas.GetSprite(name);
	}
}
