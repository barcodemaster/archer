using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/IconAtlas")]
public class IconAtlas : ScriptableObject
{
	[SerializeField] private string _folderPath = "Assets/Sprites/UpgradeIcons";
	[SerializeField] private Sprite[] _sprites;

	private Dictionary<string, Sprite> _cache;

	public Sprite GetSprite(string name)
	{
		if (_cache == null) BuildCache();
		_cache.TryGetValue(name, out Sprite s);
		return s;
	}

	private void BuildCache()
	{
		_cache = new Dictionary<string, Sprite>();
		if (_sprites == null) return;
		foreach (var s in _sprites)
			if (s != null) _cache[s.name] = s;
	}

#if UNITY_EDITOR
	[ContextMenu("Scan Folder")]
	private void ScanFolder()
	{
		var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { _folderPath });
		var list = new List<Sprite>();
		foreach (var guid in guids)
		{
			string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
			Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (s != null) list.Add(s);
		}
		_sprites = list.ToArray();
		UnityEditor.EditorUtility.SetDirty(this);
	}
#endif
}
