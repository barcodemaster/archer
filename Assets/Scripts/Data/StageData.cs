using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Data/StageData")]
public class StageData : ScriptableObject
{
	public List<MonsterSpawnInfo> spawnList;
}

[System.Serializable]
public class MonsterSpawnInfo
{
	public GameObject monsterPrefab;
	public Vector3 spawnPosition;
}
