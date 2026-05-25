using System.Collections.Generic;
using UnityEngine;
using static Define;

public class UpgradeInfo
{
	public EUpgradeType type;
	public string name;
	public string description;
	public Sprite icon;
	public int maxLevel = 1;
}

public static class UpgradeDatabase
{
	private static UpgradeInfo[] _all;

	private static void Init()
	{
		if (_all != null) return;

		TextAsset csv = Resources.Load<TextAsset>("Data/UpgradeData");
		IconAtlas atlas = Resources.Load<IconAtlas>("Data/IconAtlas");

		string[] lines = csv.text.Split('\n');
		List<UpgradeInfo> list = new();

		for (int i = 1; i < lines.Length; i++)
		{
			string line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;
			string[] cols = line.Split(',');

			list.Add(new UpgradeInfo
			{
				type = System.Enum.Parse<EUpgradeType>(cols[0].Trim()),
				name = cols[1].Trim(),
				description = cols[2].Trim(),
				icon = atlas != null ? atlas.GetSprite(cols[3].Trim()) : null,
				maxLevel = cols.Length > 4 && int.TryParse(cols[4].Trim(), out int ml) ? ml : 1,
			});
		}

		_all = list.ToArray();
	}

	public static UpgradeInfo[] GetAll()
	{
		Init();
		return _all;
	}

	public static UpgradeInfo GetInfo(EUpgradeType type)
	{
		Init();
		foreach (var info in _all)
			if (info.type == type)
				return info;
		return null;
	}

	public static List<UpgradeInfo> PickRandom(int count)
	{
		Init();
		List<UpgradeInfo> pool = new List<UpgradeInfo>();

		PlayerUpgrade playerUpgrade = Object.FindAnyObjectByType<PlayerUpgrade>();
		foreach (var info in _all)
		{
			if (playerUpgrade != null && playerUpgrade.GetLevel(info.type) >= info.maxLevel)
				continue;
			pool.Add(info);
		}

		List<UpgradeInfo> result = new List<UpgradeInfo>();

		for (int i = 0; i < count && pool.Count > 0; i++)
		{
			int idx = Random.Range(0, pool.Count);
			result.Add(pool[idx]);
			pool.RemoveAt(idx);
		}

		return result;
	}
}
