using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static Define;

/// <summary>
/// CSV에서 업적 테이블을 로드하는 정적 데이터베이스.
/// EquipmentDatabase와 동일한 CSV 파싱 패턴을 사용한다.
/// </summary>
public static class AchievementDatabase
{
	private static AchievementTable[] _all;
	private static Dictionary<int, AchievementTable> _byId;
	private static Dictionary<EAchievementType, List<AchievementTable>> _byType;

	private static void Init()
	{
		if (_all != null) return;

		TextAsset csv = Resources.Load<TextAsset>("Data/AchievementData");
		if (csv == null)
		{
#if UNITY_EDITOR
			Debug.LogError("AchievementData.csv not found in Resources/Data/");
#endif
			_all = new AchievementTable[0];
			_byId = new Dictionary<int, AchievementTable>();
			_byType = new Dictionary<EAchievementType, List<AchievementTable>>();
			return;
		}

		List<string> lines = SplitCsvRows(csv.text);
		List<AchievementTable> list = new();

		string[] headers = SplitCsvLine(lines[0]);
		Dictionary<string, int> headerMap = new();
		for (int h = 0; h < headers.Length; h++)
			headerMap[headers[h].Trim()] = h;

		for (int i = 1; i < lines.Count; i++)
		{
			string line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;
			string[] cols = SplitCsvLine(line);

			try
			{
				var table = new AchievementTable
				{
					id = GetInt(cols, headerMap, "id"),
					name = GetCol(cols, headerMap, "name"),
					description = GetCol(cols, headerMap, "description"),
					icon = GetCol(cols, headerMap, "icon"),
					type = System.Enum.Parse<EAchievementType>(GetCol(cols, headerMap, "type")),
					target = GetInt(cols, headerMap, "target"),
					rewardType = System.Enum.Parse<EAchievementReward>(GetCol(cols, headerMap, "rewardType")),
					rewardAmount = GetInt(cols, headerMap, "rewardAmount"),
				};
				list.Add(table);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"[AchievementDatabase] Failed to parse row {i}: {e.Message}");
			}
		}

		_all = list.ToArray();

		_byId = new Dictionary<int, AchievementTable>();
		_byType = new Dictionary<EAchievementType, List<AchievementTable>>();
		foreach (var t in _all)
		{
			_byId[t.id] = t;
			if (!_byType.ContainsKey(t.type))
				_byType[t.type] = new List<AchievementTable>();
			_byType[t.type].Add(t);
		}
	}

	public static AchievementTable[] GetAll()
	{
		Init();
		return _all;
	}

	public static AchievementTable GetById(int id)
	{
		Init();
		return _byId.TryGetValue(id, out var t) ? t : null;
	}

	public static List<AchievementTable> GetByType(EAchievementType type)
	{
		Init();
		return _byType.TryGetValue(type, out var list) ? list : new List<AchievementTable>();
	}

	private static string GetCol(string[] cols, Dictionary<string, int> headerMap, string key)
	{
		if (headerMap.TryGetValue(key, out int idx) && idx < cols.Length)
			return cols[idx].Trim();
		return "";
	}

	private static int GetInt(string[] cols, Dictionary<string, int> headerMap, string key)
	{
		string val = GetCol(cols, headerMap, key);
		if (int.TryParse(val, out int v)) return v;
		return 0;
	}

	private static List<string> SplitCsvRows(string text)
	{
		List<string> rows = new();
		bool inQuotes = false;
		int start = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '"') inQuotes = !inQuotes;
			else if (!inQuotes && (text[i] == '\n' || text[i] == '\r'))
			{
				if (i > start)
					rows.Add(text.Substring(start, i - start));
				start = i + 1;
			}
		}
		if (start < text.Length)
			rows.Add(text.Substring(start));
		return rows;
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
}
