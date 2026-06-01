using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static Define;

/// <summary>
/// CSV에서 장비 테이블을 로드하는 정적 데이터베이스.
/// </summary>
public static class EquipmentDatabase
{
	private static EquipmentTable[] _all;
	private static int _totalWeight;

	private static void Init()
	{
		if (_all != null) return;

		TextAsset csv = Resources.Load<TextAsset>("Data/EquipmentData");
		if (csv == null)
		{
			Debug.LogError("EquipmentData.csv not found in Resources/Data/");
			_all = new EquipmentTable[0];
			return;
		}

		List<string> lines = SplitCsvRows(csv.text);
		List<EquipmentTable> list = new();

		string[] headers = SplitCsvLine(lines[0]);
		Dictionary<string, int> headerMap = new();
		for (int h = 0; h < headers.Length; h++)
			headerMap[headers[h].Trim()] = h;

		for (int i = 1; i < lines.Count; i++)
		{
			string line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;
			string[] cols = SplitCsvLine(line);

			var table = new EquipmentTable
			{
				id = GetInt(cols, headerMap, "id"),
				name = GetCol(cols, headerMap, "name"),
				description = GetCol(cols, headerMap, "description"),
				icon = GetCol(cols, headerMap, "icon"),
				slot = System.Enum.Parse<EEquipSlot>(GetCol(cols, headerMap, "slot")),
				grade = System.Enum.Parse<EEquipGrade>(GetCol(cols, headerMap, "grade")),
				mainStatType = System.Enum.Parse<EMainStatType>(GetCol(cols, headerMap, "mainStatType")),
				baseMainStat = GetFloat(cols, headerMap, "baseMainStat"),
				mainStatPerLevel = GetFloat(cols, headerMap, "mainStatPerLevel"),
				baseSubMin = GetFloat(cols, headerMap, "baseSubMin"),
				baseSubMax = GetFloat(cols, headerMap, "baseSubMax"),
				goldCostBase = GetInt(cols, headerMap, "goldCostBase"),
				goldCostPerLevel = GetInt(cols, headerMap, "goldCostPerLevel"),
				dropWeight = GetInt(cols, headerMap, "dropWeight"),
				prefab = GetCol(cols, headerMap, "prefab"),
			};
			list.Add(table);
		}

		_all = list.ToArray();

		_totalWeight = 0;
		foreach (var t in _all)
			_totalWeight += t.dropWeight;
	}

	public static EquipmentTable[] GetAll()
	{
		Init();
		return _all;
	}

	public static EquipmentTable GetById(int id)
	{
		Init();
		foreach (var t in _all)
			if (t.id == id)
				return t;
		return null;
	}

	/// <summary>
	/// 가중치 기반 랜덤으로 장비 테이블 하나를 선택한다.
	/// </summary>
	public static EquipmentTable PickRandomDrop()
	{
		Init();
		if (_all.Length == 0) return null;

		int roll = Random.Range(0, _totalWeight);
		int cumulative = 0;
		foreach (var t in _all)
		{
			cumulative += t.dropWeight;
			if (roll < cumulative)
				return t;
		}
		return _all[_all.Length - 1];
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

	private static int GetInt(string[] cols, Dictionary<string, int> headerMap, string key)
	{
		string val = GetCol(cols, headerMap, key);
		if (int.TryParse(val, out int v))
			return v;
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
				cols.Add(line.Substring(start, i - start).Trim().Trim('"').Replace("\n", " ").Replace("\r", " "));
				start = i + 1;
			}
		}
		cols.Add(line.Substring(start).Trim().Trim('"').Replace("\n", " ").Replace("\r", " "));
		return cols.ToArray();
	}
}
