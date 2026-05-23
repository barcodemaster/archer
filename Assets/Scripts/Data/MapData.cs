using UnityEngine;
using static Define;

[CreateAssetMenu(fileName = "MapData", menuName = "Data/MapData")]
public class MapData : ScriptableObject
{
	public int width = 20;
	public int height = 20;
	public ETileType[] tiles;
	public int[] blockPrefabIndices;
	public int[] monsterPrefabIndices; // -1 = 몬스터 없음
	public Vector2Int playerSpawnPoint = new Vector2Int(1, 1);
	public Vector2Int exitPoint = new Vector2Int(-1, -1); // -1,-1 = 미설정

	/// <summary>
	/// 지정 좌표의 타일 타입을 반환한다. 범위 밖이면 Wall.
	/// </summary>
	public ETileType GetTile(int x, int z)
	{
		if (x < 0 || x >= width || z < 0 || z >= height)
			return ETileType.Wall;

		return tiles[z * width + x];
	}

	/// <summary>
	/// 지정 좌표의 몬스터 프리팹 인덱스를 반환한다. -1이면 몬스터 없음.
	/// </summary>
	public int GetMonsterIndex(int x, int z)
	{
		if (x < 0 || x >= width || z < 0 || z >= height) return -1;
		if (monsterPrefabIndices == null || monsterPrefabIndices.Length != width * height) return -1;
		return monsterPrefabIndices[z * width + x];
	}

	/// <summary>
	/// 지정 좌표의 블록 프리팹 인덱스를 반환한다. -1이면 블록 없음.
	/// </summary>
	public int GetBlockIndex(int x, int z)
	{
		if (x < 0 || x >= width || z < 0 || z >= height) return -1;
		if (blockPrefabIndices == null || blockPrefabIndices.Length != width * height) return -1;
		return blockPrefabIndices[z * width + x];
	}

	/// <summary>
	/// 배열이 null이거나 크기 불일치 시 초기화한다.
	/// </summary>
	public void InitIfNeeded()
	{
		int size = width * height;
		if (tiles == null || tiles.Length != size)
		{
			tiles = new ETileType[size];
			for (int i = 0; i < size; i++)
				tiles[i] = ETileType.Path;
		}

		if (blockPrefabIndices == null || blockPrefabIndices.Length != size)
		{
			int[] newBlocks = new int[size];
			for (int i = 0; i < size; i++)
				newBlocks[i] = (blockPrefabIndices != null && i < blockPrefabIndices.Length) ? blockPrefabIndices[i] : -1;
			blockPrefabIndices = newBlocks;
		}

		if (monsterPrefabIndices == null || monsterPrefabIndices.Length != size)
		{
			int[] newMonsters = new int[size];
			for (int i = 0; i < size; i++)
				newMonsters[i] = (monsterPrefabIndices != null && i < monsterPrefabIndices.Length) ? monsterPrefabIndices[i] : -1;
			monsterPrefabIndices = newMonsters;
		}
	}
}
