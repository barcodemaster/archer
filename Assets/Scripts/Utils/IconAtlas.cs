using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/IconAtlas")]
public class IconAtlas : ScriptableObject
{
	[SerializeField] private Sprite[] _sprites;
	private Dictionary<string, Sprite> _map;

	public Sprite GetSprite(string name)
	{
		if (_map == null)
		{
			_map = new Dictionary<string, Sprite>();
			if (_sprites != null)
				foreach (var s in _sprites)
					if (s != null)
						_map[s.name] = s;
		}
		_map.TryGetValue(name, out Sprite sprite);
		return sprite;
	}
}
