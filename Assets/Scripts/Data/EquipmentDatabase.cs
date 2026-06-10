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

	/// <summary>등급별 드롭 확률 (합계 1.0)</summary>
	private static readonly Dictionary<EEquipGrade, float> _gradeProbability = new()
	{
		{ EEquipGrade.Common,   0.60f },
		{ EEquipGrade.Uncommon, 0.25f },
		{ EEquipGrade.Rare,     0.12f },
		{ EEquipGrade.Epic,     0.03f },
	};

	/// <summary>등급별 아이템 캐시</summary>
	private static Dictionary<EEquipGrade, List<EquipmentTable>> _byGrade;

	private static void Init()
	{
		if (_all != null) return;

		TextAsset csv = Resources.Load<TextAsset>("Data/EquipmentData");
		if (csv == null)
		{
#if UNITY_EDITOR
			Debug.LogError("EquipmentData.csv not found in Resources/Data/");
#endif
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

			try
			{
				var table = new EquipmentTable
				{
					id = GetInt(cols, headerMap, "id"),
					name = GetCol(cols, headerMap, "name"),
					description = GetCol(cols, headerMap, "description"),
					icon = GetCol(cols, headerMap, "icon"),
					slot = ParseEquipSlot(GetCol(cols, headerMap, "slot")),
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
			catch (System.Exception e)
			{
				Debug.LogWarning($"[EquipmentDatabase] Failed to parse row {i}: {e.Message}");
				continue;
			}
		}

		_all = list.ToArray();

		// 등급별 아이템 캐시 구축
		_byGrade = new Dictionary<EEquipGrade, List<EquipmentTable>>();
		foreach (var t in _all)
		{
			if (!_byGrade.ContainsKey(t.grade))
				_byGrade[t.grade] = new List<EquipmentTable>();
			_byGrade[t.grade].Add(t);
		}
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
	/// 등급별 확률로 드롭 아이템을 선택한다.
	/// 1단계: 등급 결정 (Common 60%, Uncommon 25%, Rare 12%, Epic 3%)
	/// 2단계: 해당 등급 내 균등 랜덤
	/// </summary>
	public static EquipmentTable PickRandomDrop()
	{
		Init();
		if (_all.Length == 0) return null;

		// 1단계: 등급 결정
		float roll = Random.value;
		float cumulative = 0f;
		EEquipGrade selectedGrade = EEquipGrade.Common;
		foreach (var kv in _gradeProbability)
		{
			cumulative += kv.Value;
			if (roll < cumulative)
			{
				selectedGrade = kv.Key;
				break;
			}
		}

		// 2단계: 해당 등급 내 균등 랜덤
		if (_byGrade.TryGetValue(selectedGrade, out var list) && list.Count > 0)
			return list[Random.Range(0, list.Count)];

		// 해당 등급에 아이템이 없으면 전체에서 랜덤
		return _all[Random.Range(0, _all.Length)];
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

	private static EEquipSlot ParseEquipSlot(string value)
	{
		if (value == "Ring") return EEquipSlot.Ring1;
		if (value == "Pet") return EEquipSlot.Pet1;
		return System.Enum.Parse<EEquipSlot>(value);
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
