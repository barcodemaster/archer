using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using static Define;

/// <summary>
/// 타일 그리드 위에서 A* 경로를 탐색한다.
/// </summary>
public static class AStarPathfinder
{
	private struct Node
	{
		public Vector2Int pos;
		public int gCost;
		public int hCost;
		public int fCost => gCost + hCost;
		public Vector2Int parent;
	}

	private static readonly Vector2Int[] Dirs = {
		new(0, 1), new(0, -1), new(1, 0), new(-1, 0)
	};

	/// <summary>
	/// from에서 to까지 A* 경로를 탐색한다. 블록/비통과 타일을 피한다.
	/// 경로가 없으면 빈 리스트 반환. 반환 리스트는 from 제외, to 포함.
	/// </summary>
	public static List<Vector2Int> FindPath(TileMap tileMap, Vector2Int from, Vector2Int to, ETilePassFlag passFlags)
	{
		// 시작점과 도착점이 같으면 빈 리스트를 반환한다.
		if (from == to)
			return new List<Vector2Int>();

		int w = tileMap.Width;
		int h = tileMap.Height;

		// 목표가 이동 불가 타일이면 가장 가까운 이동 가능 인접 타일로 대체
		if (!IsWalkable(tileMap, to, passFlags))
		{
			Vector2Int? alt = FindNearestWalkable(tileMap, to, passFlags); // 좌표가 있을수도 없을수있다. 좌표가있으면 해당좌표, 없으면 null이다.
			if (alt == null) return new List<Vector2Int>();
			to = alt.Value;
		}

		// 지금까지 발견한 모든 노드 저장. gcost가 더 낮으면 교체한다. allNodes[(3,5)] = Node 정보, 새 gcost가 더 작으면 더 좋은경로이므로 갱신
		Dictionary<Vector2Int, Node> allNodes = new();
		// 탐색후보들. fcost, hcost, 좌표순으로 정렬. fcost가 같으면 hcost가 낮은걸 우선한다. fcost와 hcost가 같으면 좌표순으로 꺼낸다.
		SortedSet<(int fCost, int hCost, int posX, int posY)> openSet = new();

		Node startNode = new Node
		{
			pos = from,
			gCost = 0,
			hCost = ManhattanDist(from, to),
			parent = from
		};
		allNodes[from] = startNode;
		openSet.Add((startNode.fCost, startNode.hCost, from.x, from.y));

		while (openSet.Count > 0)
		{
			var current = openSet.Min;
			openSet.Remove(current);
			Vector2Int curPos = new Vector2Int(current.posX, current.posY);

			if (curPos == to)
				return ReconstructPath(allNodes, from, to);

			Node curNode = allNodes[curPos];

			foreach (Vector2Int dir in Dirs)
			{
				Vector2Int next = curPos + dir;
				if (next.x < 0 || next.x >= w || next.y < 0 || next.y >= h)
					continue;
				if (!IsWalkable(tileMap, next, passFlags))
					continue;

				int newG = curNode.gCost + 1;

				if (allNodes.TryGetValue(next, out Node existing) && existing.gCost <= newG)
					continue;

				if (allNodes.ContainsKey(next))
					openSet.Remove((existing.fCost, existing.hCost, next.x, next.y));

				Node nextNode = new Node
				{
					pos = next,
					gCost = newG,
					hCost = ManhattanDist(next, to),
					parent = curPos
				};
				allNodes[next] = nextNode;
				openSet.Add((nextNode.fCost, nextNode.hCost, next.x, next.y));
			}
		}

		return new List<Vector2Int>();
	}

	private static bool IsWalkable(TileMap tileMap, Vector2Int pos, ETilePassFlag flags)
	{
		ETileType tile = tileMap.GetTile(pos.x, pos.y);
		if (!TileMap.IsTilePassable(tile, flags))
			return false;
		if (!tileMap.CanPassBlock(pos.x, pos.y))
			return false;
		return true;
	}

	private static Vector2Int? FindNearestWalkable(TileMap tileMap, Vector2Int center, ETilePassFlag flags)
	{
		foreach (Vector2Int dir in Dirs)
		{
			Vector2Int adj = center + dir;
			if (IsWalkable(tileMap, adj, flags))
				return adj;
		}
		return null;
	}

	private static int ManhattanDist(Vector2Int a, Vector2Int b)
		=> Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

	private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Node> nodes, Vector2Int from, Vector2Int to)
	{
		List<Vector2Int> path = new();
		Vector2Int cur = to;
		while (cur != from)
		{
			path.Add(cur);
			cur = nodes[cur].parent;
		}
		path.Reverse();
		return path;
	}
}

