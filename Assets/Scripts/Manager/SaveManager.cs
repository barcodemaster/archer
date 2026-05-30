using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 세이브 엔트리: 장비 아이템 하나의 저장 데이터.
/// </summary>
[System.Serializable]
public class EquipmentSaveEntry
{
	public string uid;
	public int tableId;
	public int level;
	public ESubStatType subStatType;
	public float subStatValue;
	public bool isEquipped;
	public EEquipSlot equippedSlot;
}

/// <summary>
/// 전체 세이브 데이터.
/// </summary>
[System.Serializable]
public class SaveData
{
	public int gold;
	public List<EquipmentSaveEntry> items = new List<EquipmentSaveEntry>();
}

/// <summary>
/// JSON 기반 세이브/로드 유틸리티.
/// </summary>
public static class SaveManager
{
	private static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

	/// <summary>
	/// 현재 상태를 JSON 파일로 저장한다.
	/// </summary>
	public static void Save()
	{
		SaveData data = EquipmentManager.Instance.ToSaveData();
		string json = JsonUtility.ToJson(data, true);
		System.IO.File.WriteAllText(SavePath, json);
	}

	/// <summary>
	/// JSON 파일에서 데이터를 로드하여 매니저에 적용한다.
	/// </summary>
	public static void Load()
	{
		if (!System.IO.File.Exists(SavePath)) return;

		string json = System.IO.File.ReadAllText(SavePath);
		SaveData data = JsonUtility.FromJson<SaveData>(json);
		if (data == null) return;

		GoldManager.Instance.SetGold(data.gold);
		EquipmentManager.Instance.LoadFromSave(data);
	}
}
