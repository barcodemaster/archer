using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static Define;

public class UpgradeInfo
{
	public EUpgradeType type;
	public string name;
	public string description;
	public Sprite icon;
	public int maxLevel = 1;

	// 플레이어 스탯
	public float attack;
	public float attackPercent;
	public float attackSpeed;
	public float critRate;
	public float critDamage;
	public float critDamageBase;
	public float critDamageRange;
	public float maxHp;
	public float healAmount;
	public float scale;
	public float probability;
	public float maxLevelCap;

	// 메카닉
	public float frontArrow;
	public float multiShotDelay;
	public float ricochetRadius;

	// HP 기반
	public float attackPerHpPercent;

	// 적 디버프
	public float enemySlowPercent;
}

public static class UpgradeDatabase
{
	private static UpgradeInfo[] _all;

	private static void Init()
	{
		if (_all != null) return;

		TextAsset csv = Resources.Load<TextAsset>("Data/UpgradeData");
		if (csv == null)
		{
#if UNITY_EDITOR
			Debug.LogError("[UpgradeDatabase] Data/UpgradeData CSV not found!");
#endif
			_all = new UpgradeInfo[0];
			return;
		}
		string[] lines = csv.text.Replace("\r", "").Split('\n');
		List<UpgradeInfo> list = new();

		// 헤더 기반 파싱
		string[] headers = SplitCsvLine(lines[0].Trim());
		Dictionary<string, int> headerMap = new();
		for (int h = 0; h < headers.Length; h++)
			headerMap[headers[h].Trim()] = h;

		for (int i = 1; i < lines.Length; i++)
		{
			string line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;
			string[] cols = SplitCsvLine(line);

			try
			{
				var info = new UpgradeInfo
				{
					type = System.Enum.Parse<EUpgradeType>(GetCol(cols, headerMap, "type")),
					name = GetCol(cols, headerMap, "name"),
					description = GetCol(cols, headerMap, "description"),
					icon = IconHelper.GetSprite(GetCol(cols, headerMap, "icon")),
					maxLevel = int.TryParse(GetCol(cols, headerMap, "maxLevel"), out int ml) ? ml : 1,

					attack = GetFloat(cols, headerMap, "attack"),
					attackPercent = GetFloat(cols, headerMap, "attackPercent"),
					attackSpeed = GetFloat(cols, headerMap, "attackSpeed"),
					critRate = GetFloat(cols, headerMap, "critRate"),
					critDamage = GetFloat(cols, headerMap, "critDamage"),
					critDamageBase = GetFloat(cols, headerMap, "critDamageBase"),
					critDamageRange = GetFloat(cols, headerMap, "critDamageRange"),
					maxHp = GetFloat(cols, headerMap, "maxHp"),
					healAmount = GetFloat(cols, headerMap, "healAmount"),
					scale = GetFloat(cols, headerMap, "scale"),
					probability = GetFloat(cols, headerMap, "probability"),
					maxLevelCap = GetFloat(cols, headerMap, "maxLevelCap"),
					frontArrow = GetFloat(cols, headerMap, "frontArrow"),
					multiShotDelay = GetFloat(cols, headerMap, "multiShotDelay"),
					ricochetRadius = GetFloat(cols, headerMap, "ricochetRadius"),
					attackPerHpPercent = GetFloat(cols, headerMap, "attackPerHpPercent"),
					enemySlowPercent = GetFloat(cols, headerMap, "enemySlowPercent"),
				};
				list.Add(info);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[UpgradeDatabase] Failed to parse row {i}: {e.Message}");
				continue;
			}
		}

		_all = list.ToArray();
	}

	private static string GetCol(string[] cols, Dictionary<string, int> headerMap, string key)
	{
		if (headerMap.TryGetValue(key, out int idx) && idx < cols.Length)
			return cols[idx].Trim();
		return "";
	}

	private static float GetFloat(string[] cols, Dictionary<string, int> headerMap, string key)
	{
		string val = GetCol(cols, headerMap, key);
		if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
			return f;
		return 0f;
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

	private static string[] SplitCsvLine(string line)
	{
		List<string> cols = new();
		bool inQuotes = false;
		int start = 0;
		for (int i = 0; i < line.Length; i++)
		{
			if (line[i] == '"') inQuotes = !inQuotes;
			else if (line[i] == ',' && !inQuotes)
			{
				cols.Add(line.Substring(start, i - start).Trim().Trim('"'));
				start = i + 1;
			}
		}
		cols.Add(line.Substring(start).Trim().Trim('"'));
		return cols.ToArray();
	}

	public static List<UpgradeInfo> PickRandom(int count)
	{
		Init();
		List<UpgradeInfo> pool = new List<UpgradeInfo>();

		PlayerUpgrade playerUpgrade = PlayerController.Instance?.GetComponent<PlayerUpgrade>();
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
