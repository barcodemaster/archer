using UnityEngine;
using UnityEditor;
using static Define;

[CustomEditor(typeof(TileMap))]
public class TileMapEditor : Editor
{
	private enum EBrushMode { None, TilePaint, BlockPlace, SpikePlace, MonsterPlace, SpawnPoint, ExitPoint }

	private EBrushMode _brushMode = EBrushMode.None;
	private ETileType _tileBrush = ETileType.Path;
	private int _blockBrushIndex = 0;
	private bool _removeBlock = false;
	private bool _removeSpike = false;
	private int _monsterBrushIndex = 0;
	private bool _removeMonster = false;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		TileMap tileMap = (TileMap)target;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Map Tools", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Generate Preview"))
			tileMap.GenerateVisuals();
		if (GUILayout.Button("Clear Preview"))
			tileMap.ClearVisuals();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
		_brushMode = (EBrushMode)EditorGUILayout.EnumPopup("Brush Mode", _brushMode);

		switch (_brushMode)
		{
			case EBrushMode.TilePaint:
				string[] tileNames = { "Path", "Water", "Wall" };
				ETileType[] tileTypes = { ETileType.Path, ETileType.Water, ETileType.Wall };
				int cur = System.Array.IndexOf(tileTypes, _tileBrush);
				if (cur < 0) cur = 0;
				_tileBrush = tileTypes[EditorGUILayout.Popup("Tile Type", cur, tileNames)];
				break;
			case EBrushMode.BlockPlace:
				_removeBlock = EditorGUILayout.Toggle("Remove Block", _removeBlock);
				if (!_removeBlock && tileMap.BlockPrefabs != null && tileMap.BlockPrefabs.Length > 0)
				{
					string[] names = new string[tileMap.BlockPrefabs.Length];
					for (int i = 0; i < names.Length; i++)
						names[i] = tileMap.BlockPrefabs[i] != null ? tileMap.BlockPrefabs[i].name : $"(empty {i})";
					_blockBrushIndex = EditorGUILayout.Popup("Block Prefab", _blockBrushIndex, names);
				}
				break;
			case EBrushMode.SpikePlace:
				_removeSpike = EditorGUILayout.Toggle("Remove Spike", _removeSpike);
				break;
			case EBrushMode.MonsterPlace:
				_removeMonster = EditorGUILayout.Toggle("Remove Monster", _removeMonster);
				if (!_removeMonster && tileMap.MonsterPrefabs != null && tileMap.MonsterPrefabs.Length > 0)
				{
					string[] names = new string[tileMap.MonsterPrefabs.Length];
					for (int i = 0; i < names.Length; i++)
						names[i] = tileMap.MonsterPrefabs[i] != null ? tileMap.MonsterPrefabs[i].name : $"(empty {i})";
					_monsterBrushIndex = EditorGUILayout.Popup("Monster Prefab", _monsterBrushIndex, names);
				}
				break;
		}

		if (_brushMode != EBrushMode.None)
			EditorGUILayout.HelpBox("Click/drag in Scene view to paint. Press Escape to deselect brush.", MessageType.Info);
	}

	private void OnSceneGUI()
	{
		if (_brushMode == EBrushMode.None) return;

		TileMap tileMap = (TileMap)target;

		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

		Event e = Event.current;
		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

		Plane plane = new Plane(Vector3.up, Vector3.zero);
		if (!plane.Raycast(ray, out float distance)) return;

		Vector3 hitPoint = ray.GetPoint(distance);
		Vector2Int grid = tileMap.WorldToGrid(hitPoint);

		if (grid.x < 0 || grid.x >= tileMap.Width || grid.y < 0 || grid.y >= tileMap.Height) return;

		Vector3 cellCenter = tileMap.GridToWorld(grid.x, grid.y);
		Handles.color = Color.white;
		Handles.DrawWireCube(cellCenter, new Vector3(1f, 0.2f, 1f));

		if (_brushMode == EBrushMode.SpawnPoint)
		{
			Handles.color = Color.yellow;
			Handles.SphereHandleCap(0, cellCenter + Vector3.up * 0.5f, Quaternion.identity, 0.4f, EventType.Repaint);
		}
		else if (_brushMode == EBrushMode.ExitPoint)
		{
			Handles.color = Color.magenta;
			Handles.SphereHandleCap(0, cellCenter + Vector3.up * 0.5f, Quaternion.identity, 0.4f, EventType.Repaint);
		}

		if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
		{
			switch (_brushMode)
			{
				case EBrushMode.TilePaint:
					ETileType curTile = tileMap.GetTile(grid.x, grid.y);
					if (curTile != _tileBrush)
					{
						Undo.RecordObject(tileMap, "Paint Tile");
						tileMap.SetTile(grid.x, grid.y, _tileBrush);
						EditorUtility.SetDirty(tileMap);
						tileMap.GenerateVisuals();
					}
					break;

				case EBrushMode.BlockPlace:
					int newBlockIdx = _removeBlock ? -1 : _blockBrushIndex;
					Undo.RecordObject(tileMap, "Place Block");
					tileMap.SetBlock(grid.x, grid.y, newBlockIdx);
					EditorUtility.SetDirty(tileMap);
					tileMap.GenerateVisuals();
					break;

				case EBrushMode.SpikePlace:
					ETileType currentTile = tileMap.GetTile(grid.x, grid.y);
					bool isSpike = currentTile == ETileType.Spike;
					bool wantSpike = !_removeSpike;
					if (isSpike != wantSpike)
					{
						Undo.RecordObject(tileMap, "Place Spike");
						tileMap.SetTile(grid.x, grid.y, wantSpike ? ETileType.Spike : ETileType.Path);
						EditorUtility.SetDirty(tileMap);
						tileMap.GenerateVisuals();
					}
					break;

				case EBrushMode.MonsterPlace:
					int newMonIdx = _removeMonster ? -1 : _monsterBrushIndex;
					Undo.RecordObject(tileMap, "Place Monster");
					tileMap.SetMonster(grid.x, grid.y, newMonIdx);
					EditorUtility.SetDirty(tileMap);
					break;

				case EBrushMode.SpawnPoint:
					Undo.RecordObject(tileMap, "Set Spawn Point");
					tileMap.SetSpawnPoint(grid);
					EditorUtility.SetDirty(tileMap);
					break;

				case EBrushMode.ExitPoint:
					Undo.RecordObject(tileMap, "Set Exit Point");
					tileMap.SetExitPoint(grid);
					EditorUtility.SetDirty(tileMap);
					tileMap.GenerateVisuals();
					break;
			}

			e.Use();
		}

		SceneView.RepaintAll();
	}
}
