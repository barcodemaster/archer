using UnityEngine;
using UnityEditor;
using static Define;

[CustomEditor(typeof(TileMap))]
public class TileMapEditor : Editor
{
	private enum EBrushMode { None, TilePaint, BlockPlace, MonsterPlace, SpawnPoint, ExitPoint }

	private EBrushMode _brushMode = EBrushMode.None;
	private ETileType _tileBrush = ETileType.Path;
	private int _blockBrushIndex = 0;
	private bool _removeBlock = false;
	private int _monsterBrushIndex = 0;
	private bool _removeMonster = false;

	private bool _showRandomSettings = false;
	private MapGenerationSettings _randomSettings = new MapGenerationSettings();

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		TileMap tileMap = (TileMap)target;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Map Tools", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Generate Preview"))
		{
			if (tileMap.MapData != null)
			{
				tileMap.MapData.InitIfNeeded();
				tileMap.GenerateVisuals();
			}
		}
		if (GUILayout.Button("Clear Preview"))
		{
			tileMap.ClearVisuals();
		}
		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Generate Random"))
		{
			if (tileMap.MapData != null)
			{
				Undo.RecordObject(tileMap.MapData, "Generate Random Map");
				tileMap.GenerateRandomMap(_randomSettings);
				EditorUtility.SetDirty(tileMap.MapData);
				tileMap.GenerateVisuals();
			}
		}

		_showRandomSettings = EditorGUILayout.Foldout(_showRandomSettings, "Random Settings");
		if (_showRandomSettings)
		{
			EditorGUI.indentLevel++;
			_randomSettings.waterRegionCount = EditorGUILayout.IntField("Water Regions", _randomSettings.waterRegionCount);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Water Size");
			_randomSettings.waterMinSize = EditorGUILayout.IntField(_randomSettings.waterMinSize, GUILayout.Width(40));
			EditorGUILayout.LabelField("~", GUILayout.Width(14));
			_randomSettings.waterMaxSize = EditorGUILayout.IntField(_randomSettings.waterMaxSize, GUILayout.Width(40));
			EditorGUILayout.EndHorizontal();
			_randomSettings.blockDensity = EditorGUILayout.Slider("Block Density", _randomSettings.blockDensity, 0f, 0.3f);
			_randomSettings.seed = EditorGUILayout.IntField("Seed (0=rand)", _randomSettings.seed);
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
		_brushMode = (EBrushMode)EditorGUILayout.EnumPopup("Brush Mode", _brushMode);

		switch (_brushMode)
		{
			case EBrushMode.TilePaint:
				string[] tileNames = { "Path", "Water" };
				ETileType[] tileTypes = { ETileType.Path, ETileType.Water };
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
		MapData mapData = tileMap.MapData;
		if (mapData == null) return;
		mapData.InitIfNeeded();

		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

		Event e = Event.current;
		Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

		Plane plane = new Plane(Vector3.up, Vector3.zero);
		if (!plane.Raycast(ray, out float distance)) return;

		Vector3 hitPoint = ray.GetPoint(distance);
		Vector2Int grid = tileMap.WorldToGrid(hitPoint);

		if (grid.x < 0 || grid.x >= mapData.width || grid.y < 0 || grid.y >= mapData.height) return;

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
			int idx = grid.y * mapData.width + grid.x;

			switch (_brushMode)
			{
				case EBrushMode.TilePaint:
					if (mapData.tiles[idx] != _tileBrush)
					{
						Undo.RecordObject(mapData, "Paint Tile");
						mapData.tiles[idx] = _tileBrush;
						EditorUtility.SetDirty(mapData);
						tileMap.GenerateVisuals();
					}
					break;

				case EBrushMode.BlockPlace:
					int newBlockIdx = _removeBlock ? -1 : _blockBrushIndex;
					if (mapData.blockPrefabIndices[idx] != newBlockIdx)
					{
						Undo.RecordObject(mapData, "Place Block");
						mapData.blockPrefabIndices[idx] = newBlockIdx;
						EditorUtility.SetDirty(mapData);
						tileMap.GenerateVisuals();
					}
					break;

				case EBrushMode.MonsterPlace:
					int newMonIdx = _removeMonster ? -1 : _monsterBrushIndex;
					if (mapData.monsterPrefabIndices[idx] != newMonIdx)
					{
						Undo.RecordObject(mapData, "Place Monster");
						mapData.monsterPrefabIndices[idx] = newMonIdx;
						EditorUtility.SetDirty(mapData);
					}
					break;

				case EBrushMode.SpawnPoint:
					if (mapData.playerSpawnPoint != grid)
					{
						Undo.RecordObject(mapData, "Set Spawn Point");
						mapData.playerSpawnPoint = grid;
						EditorUtility.SetDirty(mapData);
					}
					break;

				case EBrushMode.ExitPoint:
					if (mapData.exitPoint != grid)
					{
						Undo.RecordObject(mapData, "Set Exit Point");
						mapData.exitPoint = grid;
						EditorUtility.SetDirty(mapData);
						tileMap.GenerateVisuals();
					}
					break;
			}

			e.Use();
		}

		SceneView.RepaintAll();
	}
}
