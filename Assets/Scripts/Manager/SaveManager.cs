using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static Define;

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

[System.Serializable]
public class AchievementProgressEntry
{
	public Define.EAchievementType type;
	public int value;
}

[System.Serializable]
public class AchievementSaveData
{
	public List<AchievementProgressEntry> progress = new List<AchievementProgressEntry>();
	public List<int> unlockedIds = new List<int>();
}

[System.Serializable]
public class SaveData
{
	public int version = 1;
	public int gold;
	public int bestStageIndex;
	public List<EquipmentSaveEntry> items = new List<EquipmentSaveEntry>();
	public AchievementSaveData achievements = new AchievementSaveData();
	public string checksum;
}

public static class SaveManager
{
	private const int CURRENT_VERSION = 2;
	private static string SavePath => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

	private static int _bestStageIndex;
	public static int BestStageIndex => _bestStageIndex;

	public static void UpdateBestStage(int stageIndex)
	{
		if (stageIndex > _bestStageIndex)
		{
			_bestStageIndex = stageIndex;
			Save();
		}
	}

	public static void Save()
	{
		try
		{
			SaveData data = EquipmentManager.Instance.ToSaveData();
			data.version = CURRENT_VERSION;
			data.bestStageIndex = _bestStageIndex;
			data.achievements = AchievementManager.Instance.ToSaveData();
			data.checksum = "";
			string jsonForHash = JsonUtility.ToJson(data, false);
			data.checksum = ComputeChecksum(jsonForHash);
			string json = JsonUtility.ToJson(data, true);
			System.IO.File.WriteAllText(SavePath, json);
		}
		catch (System.Exception e)
		{
			Logger.LogError("SaveManager", $"Save failed: {e.Message}");
		}
	}

	public static void Load()
	{
		try
		{
			if (!System.IO.File.Exists(SavePath)) return;

			string json = System.IO.File.ReadAllText(SavePath);
			SaveData data = JsonUtility.FromJson<SaveData>(json);
			if (data == null)
			{
				Logger.LogWarning("SaveManager", "Save data is null or corrupt.");
				return;
			}

			// Checksum 검증
			string savedChecksum = data.checksum;
			data.checksum = "";
			string jsonForHash = JsonUtility.ToJson(data, false);
			string computed = ComputeChecksum(jsonForHash);
			if (!string.IsNullOrEmpty(savedChecksum) && savedChecksum != computed)
			{
				Logger.LogWarning("SaveManager", "Save data checksum mismatch. Data may be tampered.");
			}

			// 버전 마이그레이션
			MigrateIfNeeded(data);

			GoldManager.Instance.SetGold(data.gold);
			_bestStageIndex = data.bestStageIndex;
			EquipmentManager.Instance.LoadFromSave(data);
			AchievementManager.Instance.LoadFromSave(data.achievements);
		}
		catch (System.Exception e)
		{
			Logger.LogError("SaveManager", $"Load failed: {e.Message}");
		}
	}

	private static void MigrateIfNeeded(SaveData data)
	{
		if (data.version < 2)
		{
			Logger.Log("SaveManager", $"Migrating save from v{data.version} to v{CURRENT_VERSION}");
			if (data.achievements == null)
				data.achievements = new AchievementSaveData();
		}
		data.version = CURRENT_VERSION;
	}

	private static string ComputeChecksum(string input)
	{
		using (SHA256 sha256 = SHA256.Create())
		{
			byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.Append(b.ToString("x2"));
			return sb.ToString();
		}
	}
}
