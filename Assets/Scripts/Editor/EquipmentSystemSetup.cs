using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using TMPro;

/// <summary>
/// Unity 에디터에서 장비 시스템 UI 계층 구조와 Inspector 연결을 자동 생성하는 셋업 도구.
/// Menu: Tools > Equipment System Setup
/// </summary>
public class EquipmentSystemSetup : EditorWindow
{
	[MenuItem("Tools/Equipment System Setup")]
	public static void ShowWindow()
	{
		GetWindow<EquipmentSystemSetup>("Equipment Setup");
	}

	private void OnGUI()
	{
		GUILayout.Label("Equipment System Setup", EditorStyles.boldLabel);
		GUILayout.Space(10);

		if (GUILayout.Button("1. Create UI_ItemSlot Prefab"))
			CreateItemSlotPrefab();

		GUILayout.Space(5);

		if (GUILayout.Button("2. Setup EquipmentLayer in Scene"))
			SetupEquipmentLayer();

		GUILayout.Space(5);

		if (GUILayout.Button("3. Setup ModelCamera & SpawnPoint"))
			SetupModelCameraAndSpawnPoint();

		GUILayout.Space(5);

		if (GUILayout.Button("4. Wire UIManager & StageManager"))
			WireManagers();

		GUILayout.Space(5);

		if (GUILayout.Button("5. Add Equipment Button to HUD"))
			AddEquipmentButton();

		GUILayout.Space(20);
		GUI.color = Color.green;
		if (GUILayout.Button("Run ALL Steps", GUILayout.Height(40)))
		{
			CreateItemSlotPrefab();
			SetupEquipmentLayer();
			SetupModelCameraAndSpawnPoint();
			WireManagers();
			AddEquipmentButton();
			Debug.Log("[EquipmentSetup] All steps completed!");
		}
		GUI.color = Color.white;

		GUILayout.Space(10);

		if (GUILayout.Button("6. Setup Main Camera Culling Mask"))
			SetupMainCameraCulling();
	}

	/// <summary>
	/// UI_ItemSlot 프리팹을 생성한다.
	/// </summary>
	private static void CreateItemSlotPrefab()
	{
		string path = "Assets/Resources/Prefabs/UI_ItemSlot.prefab";
		if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
		{
			Debug.Log("[EquipmentSetup] UI_ItemSlot prefab already exists. Skipping.");
			return;
		}

		// 임시 Canvas 생성 (UI 요소는 Canvas 아래에서 생성해야 RectTransform이 자동 생성됨)
		GameObject tempCanvas = new GameObject("TempCanvas");
		Canvas canvas = tempCanvas.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;

		GameObject root = CreateUIObject("UI_ItemSlot", tempCanvas.transform);
		RectTransform rootRt = root.GetComponent<RectTransform>();
		rootRt.sizeDelta = new Vector2(120, 120);
		Image rootImage = root.AddComponent<Image>();
		rootImage.color = Color.clear;
		Button btn = root.AddComponent<Button>();
		btn.targetGraphic = rootImage;
		UI_ItemSlot itemSlot = root.AddComponent<UI_ItemSlot>();

		// Background (등급별 배경 이미지)
		GameObject bg = CreateUIObject("Background", root.transform);
		Image bgImage = bg.AddComponent<Image>();
		bgImage.color = new Color(0.5f, 0.5f, 0.5f);
		StretchFill(bg.GetComponent<RectTransform>());

		// Icon (중앙 정렬 80x80)
		GameObject icon = CreateUIObject("Icon", root.transform);
		Image iconImage = icon.AddComponent<Image>();
		RectTransform iconRt = icon.GetComponent<RectTransform>();
		iconRt.anchorMin = new Vector2(0.5f, 0.5f);
		iconRt.anchorMax = new Vector2(0.5f, 0.5f);
		iconRt.pivot = new Vector2(0.5f, 0.5f);
		iconRt.anchoredPosition = Vector2.zero;
		iconRt.sizeDelta = new Vector2(80, 80);

		// LevelText (하단 작게)
		GameObject lvObj = CreateUIObject("LevelText", root.transform);
		TextMeshProUGUI lvText = lvObj.AddComponent<TextMeshProUGUI>();
		lvText.text = "Lv.1";
		lvText.fontSize = 12;
		lvText.alignment = TextAlignmentOptions.Center;
		RectTransform lvRt = lvObj.GetComponent<RectTransform>();
		lvRt.anchorMin = new Vector2(0, 0);
		lvRt.anchorMax = new Vector2(1, 0.22f);
		lvRt.offsetMin = Vector2.zero;
		lvRt.offsetMax = Vector2.zero;

		// SerializedField 연결
		SerializedObject so = new SerializedObject(itemSlot);
		so.FindProperty("_bgImage").objectReferenceValue = bgImage;
		so.FindProperty("_iconImage").objectReferenceValue = iconImage;
		so.FindProperty("_levelText").objectReferenceValue = lvText;
		so.ApplyModifiedPropertiesWithoutUndo();

		// 프리팹 저장
		root.transform.SetParent(null);
		DestroyImmediate(tempCanvas);
		PrefabUtility.SaveAsPrefabAsset(root, path);
		DestroyImmediate(root);

		Debug.Log("[EquipmentSetup] UI_ItemSlot prefab created at " + path);
	}

	/// <summary>
	/// EquipmentLayer 전체 계층 구조를 씬 Canvas 아래에 생성한다.
	/// </summary>
	private static void SetupEquipmentLayer()
	{
		Canvas canvas = FindMainCanvas();
		if (canvas == null)
		{
			Debug.LogError("[EquipmentSetup] Canvas not found in scene!");
			return;
		}

		// 이미 있으면 스킵
		Transform existing = canvas.transform.Find("EquipmentLayer");
		if (existing != null)
		{
			Debug.Log("[EquipmentSetup] EquipmentLayer already exists. Skipping.");
			return;
		}

		// Root: EquipmentLayer
		GameObject eqLayer = CreateUIObject("EquipmentLayer", canvas.transform);
		StretchFill(eqLayer.GetComponent<RectTransform>());
		UI_EquipmentPanel panel = eqLayer.AddComponent<UI_EquipmentPanel>();

		// Panel (반투명 배경)
		GameObject panelBg = CreateUIObject("Panel", eqLayer.transform);
		Image panelImg = panelBg.AddComponent<Image>();
		panelImg.color = new Color(0, 0, 0, 0.85f);
		StretchFill(panelBg.GetComponent<RectTransform>());

		// === TopSection ===
		GameObject topSection = CreateUIObject("TopSection", panelBg.transform);
		RectTransform topRt = topSection.GetComponent<RectTransform>();
		topRt.anchorMin = new Vector2(0, 0.5f);
		topRt.anchorMax = new Vector2(1, 1);
		topRt.offsetMin = Vector2.zero;
		topRt.offsetMax = Vector2.zero;

		// PlayerModelImage (RawImage)
		GameObject modelImg = CreateUIObject("PlayerModelImage", topSection.transform);
		RawImage rawImg = modelImg.AddComponent<RawImage>();
		RectTransform modelRt = modelImg.GetComponent<RectTransform>();
		modelRt.anchorMin = new Vector2(0.5f, 0.5f);
		modelRt.anchorMax = new Vector2(0.5f, 0.5f);
		modelRt.sizeDelta = new Vector2(300, 400);
		modelRt.anchoredPosition = Vector2.zero;

		// LeftSlots
		GameObject leftSlots = CreateUIObject("LeftSlots", topSection.transform);
		VerticalLayoutGroup leftVlg = leftSlots.AddComponent<VerticalLayoutGroup>();
		leftVlg.spacing = 10;
		leftVlg.childForceExpandWidth = false;
		leftVlg.childForceExpandHeight = false;
		leftVlg.childAlignment = TextAnchor.UpperCenter;
		RectTransform leftRt = leftSlots.GetComponent<RectTransform>();
		leftRt.anchorMin = new Vector2(0, 0.1f);
		leftRt.anchorMax = new Vector2(0.25f, 0.9f);
		leftRt.offsetMin = new Vector2(10, 0);
		leftRt.offsetMax = new Vector2(0, 0);

		string[] leftSlotNames = { "Weapon", "Top", "Bottom", "Bracelet" };
		int[] leftSlotEnumValues = { 0, 1, 2, 3 }; // EEquipSlot enum values

		UI_EquipSlot[] allEquipSlots = new UI_EquipSlot[8];

		for (int i = 0; i < leftSlotNames.Length; i++)
		{
			allEquipSlots[i] = CreateEquipSlot(leftSlots.transform, leftSlotNames[i], leftSlotEnumValues[i]);
		}

		// RightSlots
		GameObject rightSlots = CreateUIObject("RightSlots", topSection.transform);
		VerticalLayoutGroup rightVlg = rightSlots.AddComponent<VerticalLayoutGroup>();
		rightVlg.spacing = 10;
		rightVlg.childForceExpandWidth = false;
		rightVlg.childForceExpandHeight = false;
		rightVlg.childAlignment = TextAnchor.UpperCenter;
		RectTransform rightRt = rightSlots.GetComponent<RectTransform>();
		rightRt.anchorMin = new Vector2(0.75f, 0.1f);
		rightRt.anchorMax = new Vector2(1, 0.9f);
		rightRt.offsetMin = new Vector2(0, 0);
		rightRt.offsetMax = new Vector2(-10, 0);

		string[] rightSlotNames = { "Ring", "Ring", "Pet", "Pet" };
		int[] rightSlotEnumValues = { 4, 5, 6, 7 };

		for (int i = 0; i < rightSlotNames.Length; i++)
		{
			allEquipSlots[i + 4] = CreateEquipSlot(rightSlots.transform, rightSlotNames[i], rightSlotEnumValues[i]);
		}

		// StatsBar
		GameObject statsBar = CreateUIObject("StatsBar", topSection.transform);
		HorizontalLayoutGroup statsHlg = statsBar.AddComponent<HorizontalLayoutGroup>();
		statsHlg.spacing = 20;
		statsHlg.childForceExpandWidth = true;
		statsHlg.childForceExpandHeight = false;
		statsHlg.childAlignment = TextAnchor.MiddleCenter;
		RectTransform statsRt = statsBar.GetComponent<RectTransform>();
		statsRt.anchorMin = new Vector2(0.1f, 0);
		statsRt.anchorMax = new Vector2(0.9f, 0.1f);
		statsRt.offsetMin = Vector2.zero;
		statsRt.offsetMax = Vector2.zero;

		TextMeshProUGUI atkText = CreateTMPChild(statsBar.transform, "AttackText", "ATK +0", 16);
		TextMeshProUGUI hpText = CreateTMPChild(statsBar.transform, "HpText", "HP +0", 16);
		TextMeshProUGUI goldText = CreateTMPChild(statsBar.transform, "GoldText", "0G", 16);

		// === BottomSection ===
		GameObject bottomSection = CreateUIObject("BottomSection", panelBg.transform);
		RectTransform bottomRt = bottomSection.GetComponent<RectTransform>();
		bottomRt.anchorMin = new Vector2(0, 0);
		bottomRt.anchorMax = new Vector2(1, 0.5f);
		bottomRt.offsetMin = new Vector2(10, 10);
		bottomRt.offsetMax = new Vector2(-10, -10);

		// InventoryScroll
		GameObject scrollObj = CreateUIObject("InventoryScroll", bottomSection.transform);
		StretchFill(scrollObj.GetComponent<RectTransform>());
		ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Elastic;

		// Viewport
		GameObject viewport = CreateUIObject("Viewport", scrollObj.transform);
		StretchFill(viewport.GetComponent<RectTransform>());
		Image viewportImg = viewport.AddComponent<Image>();
		viewportImg.color = Color.white;
		Mask mask = viewport.AddComponent<Mask>();
		mask.showMaskGraphic = false;

		// InventoryContent
		GameObject content = CreateUIObject("InventoryContent", viewport.transform);
		RectTransform contentRt = content.GetComponent<RectTransform>();
		contentRt.anchorMin = new Vector2(0, 1);
		contentRt.anchorMax = new Vector2(1, 1);
		contentRt.pivot = new Vector2(0.5f, 1);
		contentRt.offsetMin = new Vector2(0, 0);
		contentRt.offsetMax = new Vector2(0, 0);
		GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
		grid.cellSize = new Vector2(120, 120);
		grid.spacing = new Vector2(10, 10);
		grid.startAxis = GridLayoutGroup.Axis.Horizontal;
		grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		grid.constraintCount = 5;
		grid.childAlignment = TextAnchor.UpperLeft;
		ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
		csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		scrollRect.content = contentRt;
		scrollRect.viewport = viewport.GetComponent<RectTransform>();

		// CloseButton
		GameObject closeBtn = CreateUIObject("CloseButton", panelBg.transform);
		Button closeBtnComp = closeBtn.AddComponent<Button>();
		Image closeBtnImg = closeBtn.AddComponent<Image>();
		closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1);
		RectTransform closeBtnRt = closeBtn.GetComponent<RectTransform>();
		closeBtnRt.anchorMin = new Vector2(1, 1);
		closeBtnRt.anchorMax = new Vector2(1, 1);
		closeBtnRt.pivot = new Vector2(1, 1);
		closeBtnRt.anchoredPosition = new Vector2(-10, -10);
		closeBtnRt.sizeDelta = new Vector2(60, 60);

		TextMeshProUGUI closeTxt = CreateTMPChild(closeBtn.transform, "Text", "X", 24);
		closeTxt.alignment = TextAlignmentOptions.Center;
		StretchFill(closeTxt.GetComponent<RectTransform>());

		// === DetailPopup ===
		GameObject detailPopup = CreateDetailPopup(panelBg.transform);

		// Wire UI_EquipmentPanel
		SerializedObject panelSo = new SerializedObject(panel);
		SerializedProperty slotsArr = panelSo.FindProperty("_equipSlots");
		slotsArr.arraySize = 8;
		for (int i = 0; i < 8; i++)
			slotsArr.GetArrayElementAtIndex(i).objectReferenceValue = allEquipSlots[i];

		panelSo.FindProperty("_inventoryContent").objectReferenceValue = content.transform;

		GameObject itemSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI_ItemSlot.prefab");
		panelSo.FindProperty("_itemSlotPrefab").objectReferenceValue = itemSlotPrefab;

		panelSo.FindProperty("_attackText").objectReferenceValue = atkText;
		panelSo.FindProperty("_hpText").objectReferenceValue = hpText;
		UI_ItemDetailPopup detailComp = detailPopup.GetComponent<UI_ItemDetailPopup>();
		panelSo.FindProperty("_detailPopup").objectReferenceValue = detailComp;
		panelSo.FindProperty("_playerModelImage").objectReferenceValue = rawImg;
		panelSo.FindProperty("_closeButton").objectReferenceValue = closeBtnComp;
		panelSo.ApplyModifiedPropertiesWithoutUndo();

		// 기본 비활성
		detailPopup.SetActive(false);
		eqLayer.SetActive(false);

		Undo.RegisterCreatedObjectUndo(eqLayer, "Create EquipmentLayer");
		Debug.Log("[EquipmentSetup] EquipmentLayer created and wired.");
	}

	/// <summary>
	/// DetailPopup 계층 구조를 생성하고 Inspector를 연결한다.
	/// </summary>
	private static GameObject CreateDetailPopup(Transform parent)
	{
		GameObject popup = CreateUIObject("DetailPopup", parent);
		RectTransform popupRt = popup.GetComponent<RectTransform>();
		popupRt.anchorMin = new Vector2(0.1f, 0.15f);
		popupRt.anchorMax = new Vector2(0.9f, 0.85f);
		popupRt.offsetMin = Vector2.zero;
		popupRt.offsetMax = Vector2.zero;
		UI_ItemDetailPopup detailComp = popup.AddComponent<UI_ItemDetailPopup>();

		// PopupBg
		GameObject popupBg = CreateUIObject("PopupBg", popup.transform);
		Image popupBgImg = popupBg.AddComponent<Image>();
		popupBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
		StretchFill(popupBg.GetComponent<RectTransform>());

		// GradeBg
		GameObject gradeBg = CreateUIObject("GradeBg", popup.transform);
		Image gradeBgImg = gradeBg.AddComponent<Image>();
		gradeBgImg.color = Color.white;
		RectTransform gradeBgRt = gradeBg.GetComponent<RectTransform>();
		gradeBgRt.anchorMin = new Vector2(0.05f, 0.7f);
		gradeBgRt.anchorMax = new Vector2(0.35f, 0.95f);
		gradeBgRt.offsetMin = Vector2.zero;
		gradeBgRt.offsetMax = Vector2.zero;

		// IconImage
		GameObject iconObj = CreateUIObject("IconImage", popup.transform);
		Image iconImg = iconObj.AddComponent<Image>();
		RectTransform iconRt = iconObj.GetComponent<RectTransform>();
		iconRt.anchorMin = new Vector2(0.1f, 0.72f);
		iconRt.anchorMax = new Vector2(0.1f, 0.72f);
		iconRt.pivot = new Vector2(0, 0);
		iconRt.sizeDelta = new Vector2(80, 80);

		// Texts
		TextMeshProUGUI nameTxt = CreateTMPChild(popup.transform, "NameText", "Item Name", 22);
		SetAnchoredRect(nameTxt, 0.4f, 0.85f, 0.95f, 0.95f);

		TextMeshProUGUI gradeTxt = CreateTMPChild(popup.transform, "GradeText", "Common", 16);
		SetAnchoredRect(gradeTxt, 0.4f, 0.78f, 0.95f, 0.85f);

		TextMeshProUGUI lvTxt = CreateTMPChild(popup.transform, "LevelText", "Lv.1", 16);
		SetAnchoredRect(lvTxt, 0.4f, 0.71f, 0.95f, 0.78f);

		TextMeshProUGUI mainStatTxt = CreateTMPChild(popup.transform, "MainStatText", "ATK +50", 18);
		SetAnchoredRect(mainStatTxt, 0.05f, 0.58f, 0.95f, 0.68f);

		TextMeshProUGUI subStatTxt = CreateTMPChild(popup.transform, "SubStatText", "CritChance +3.2%", 16);
		SetAnchoredRect(subStatTxt, 0.05f, 0.48f, 0.95f, 0.58f);

		TextMeshProUGUI costTxt = CreateTMPChild(popup.transform, "CostText", "Level Up: 100G", 16);
		SetAnchoredRect(costTxt, 0.05f, 0.38f, 0.95f, 0.48f);

		// EquipButton
		GameObject equipBtn = CreateButtonWithText(popup.transform, "EquipButton", "Equip",
			new Vector2(0.05f, 0.05f), new Vector2(0.35f, 0.18f), new Color(0.2f, 0.6f, 0.2f, 1));
		TextMeshProUGUI equipBtnText = equipBtn.GetComponentInChildren<TextMeshProUGUI>();

		// LevelUpButton
		GameObject lvUpBtn = CreateButtonWithText(popup.transform, "LevelUpButton", "Level Up",
			new Vector2(0.38f, 0.05f), new Vector2(0.68f, 0.18f), new Color(0.2f, 0.4f, 0.8f, 1));

		// PopupCloseButton
		GameObject popCloseBtn = CreateButtonWithText(popup.transform, "PopupCloseButton", "X",
			new Vector2(0.71f, 0.05f), new Vector2(0.95f, 0.18f), new Color(0.8f, 0.2f, 0.2f, 1));

		// Wire UI_ItemDetailPopup
		SerializedObject detailSo = new SerializedObject(detailComp);
		detailSo.FindProperty("_iconImage").objectReferenceValue = iconImg;
		detailSo.FindProperty("_gradeBg").objectReferenceValue = gradeBgImg;
		detailSo.FindProperty("_nameText").objectReferenceValue = nameTxt;
		detailSo.FindProperty("_gradeText").objectReferenceValue = gradeTxt;
		detailSo.FindProperty("_levelText").objectReferenceValue = lvTxt;
		detailSo.FindProperty("_statsText").objectReferenceValue = mainStatTxt;
		detailSo.FindProperty("_descriptionText").objectReferenceValue = subStatTxt;
		detailSo.FindProperty("_costText").objectReferenceValue = costTxt;
		detailSo.FindProperty("_equipButton").objectReferenceValue = equipBtn.GetComponent<Button>();
		detailSo.FindProperty("_equipButtonText").objectReferenceValue = equipBtnText;
		detailSo.FindProperty("_levelUpButton").objectReferenceValue = lvUpBtn.GetComponent<Button>();
		detailSo.FindProperty("_closeButton").objectReferenceValue = popCloseBtn.GetComponent<Button>();
		detailSo.ApplyModifiedPropertiesWithoutUndo();

		return popup;
	}

	/// <summary>
	/// EquipModelCamera와 EquipModelSpawnPoint를 씬 루트에 생성한다.
	/// </summary>
	private static void SetupModelCameraAndSpawnPoint()
	{
		// ModelCamera
		if (GameObject.Find("EquipModelCamera") == null)
		{
			GameObject camObj = new GameObject("EquipModelCamera");
			camObj.transform.position = new Vector3(100, 0.8f, 100);
			camObj.transform.rotation = Quaternion.identity;

			Camera cam = camObj.AddComponent<Camera>();
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0, 0, 0, 0);
			int eqModelLayer = LayerMask.NameToLayer("EquipmentModel");
			if (eqModelLayer >= 0)
				cam.cullingMask = 1 << eqModelLayer;
			else
				cam.cullingMask = 0;
			cam.fieldOfView = 30;
			cam.depth = -2;
			cam.enabled = false;

			// URP 카메라 설정
			var urpCamData = camObj.GetComponent<UniversalAdditionalCameraData>();
			if (urpCamData == null)
				urpCamData = camObj.AddComponent<UniversalAdditionalCameraData>();
			urpCamData.renderType = CameraRenderType.Base;
			urpCamData.renderPostProcessing = false;

			// 오디오 리스너 제거
			AudioListener al = camObj.GetComponent<AudioListener>();
			if (al != null) DestroyImmediate(al);

			Undo.RegisterCreatedObjectUndo(camObj, "Create EquipModelCamera");

			// Wire to EquipmentPanel
			WireModelCameraToPanel(cam);

			Debug.Log("[EquipmentSetup] EquipModelCamera created at (100, 0.8, 100)");
		}
		else
		{
			Debug.Log("[EquipmentSetup] EquipModelCamera already exists.");
		}

		// ModelSpawnPoint
		if (GameObject.Find("EquipModelSpawnPoint") == null)
		{
			GameObject spawnPoint = new GameObject("EquipModelSpawnPoint");
			spawnPoint.transform.position = new Vector3(100, 0, 102.5f);
			spawnPoint.transform.rotation = Quaternion.Euler(0, 180, 0);

			Undo.RegisterCreatedObjectUndo(spawnPoint, "Create EquipModelSpawnPoint");

			// Wire to EquipmentPanel
			WireModelSpawnPointToPanel(spawnPoint.transform);

			Debug.Log("[EquipmentSetup] EquipModelSpawnPoint created at (100, 0, 102.5)");
		}
		else
		{
			Debug.Log("[EquipmentSetup] EquipModelSpawnPoint already exists.");
		}

		// EquipModel 전용 조명
		if (GameObject.Find("EquipModelLight") == null)
		{
			GameObject lightObj = new GameObject("EquipModelLight");
			lightObj.transform.position = new Vector3(100, 3, 101);
			lightObj.transform.rotation = Quaternion.Euler(30, 0, 0);
			Light light = lightObj.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.5f;
			light.color = Color.white;
			int eqModelLayer = LayerMask.NameToLayer("EquipmentModel");
			if (eqModelLayer >= 0)
				light.cullingMask = 1 << eqModelLayer;

			Undo.RegisterCreatedObjectUndo(lightObj, "Create EquipModelLight");
			Debug.Log("[EquipmentSetup] EquipModelLight created.");
		}
	}

	/// <summary>
	/// UIManager와 StageManager의 Inspector 필드를 연결한다.
	/// </summary>
	private static void WireManagers()
	{
		// UIManager - _equipmentLayer
		UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
		if (uiMgr != null)
		{
			Canvas canvas = FindMainCanvas();
			if (canvas != null)
			{
				Transform eqLayer = canvas.transform.Find("EquipmentLayer");
				if (eqLayer != null)
				{
					SerializedObject uiSo = new SerializedObject(uiMgr);
					uiSo.FindProperty("_equipmentLayer").objectReferenceValue = eqLayer.gameObject;
					uiSo.ApplyModifiedPropertiesWithoutUndo();
					Debug.Log("[EquipmentSetup] UIManager._equipmentLayer wired.");
				}
			}
		}
		else
		{
			Debug.LogWarning("[EquipmentSetup] UIManager not found in scene.");
		}

		// StageManager - orb prefabs
		StageManager stageMgr = Object.FindAnyObjectByType<StageManager>();
		if (stageMgr != null)
		{
			SerializedObject stageSo = new SerializedObject(stageMgr);

			GameObject goldOrb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/GoldOrb.prefab");
			if (goldOrb != null)
				stageSo.FindProperty("_goldOrbPrefab").objectReferenceValue = goldOrb;

			// 주의: 프리팹 이름에 오타 있음 (EquipmenetOrb)
			GameObject eqOrb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/EquipmenetOrb.prefab");
			if (eqOrb == null)
				eqOrb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/EquipmentOrb.prefab");
			if (eqOrb != null)
				stageSo.FindProperty("_equipmentOrbPrefab").objectReferenceValue = eqOrb;

			stageSo.ApplyModifiedPropertiesWithoutUndo();
			Debug.Log("[EquipmentSetup] StageManager orb prefabs wired.");
		}
		else
		{
			Debug.LogWarning("[EquipmentSetup] StageManager not found in scene.");
		}
	}

	/// <summary>
	/// JoystickLayer에 장비창 열기 버튼을 추가한다.
	/// </summary>
	private static void AddEquipmentButton()
	{
		Canvas canvas = FindMainCanvas();
		if (canvas == null) return;

		Transform joystickLayer = canvas.transform.Find("JoystickLayer");
		if (joystickLayer == null)
		{
			Debug.LogWarning("[EquipmentSetup] JoystickLayer not found.");
			return;
		}

		Transform existingBtn = joystickLayer.Find("EquipmentButton");
		if (existingBtn != null)
		{
			Debug.Log("[EquipmentSetup] EquipmentButton already exists.");
			return;
		}

		GameObject btnObj = CreateUIObject("EquipmentButton", joystickLayer);
		Button btn = btnObj.AddComponent<Button>();
		Image btnImg = btnObj.AddComponent<Image>();
		btnImg.color = new Color(0.3f, 0.3f, 0.5f, 0.8f);

		RectTransform btnRt = btnObj.GetComponent<RectTransform>();
		btnRt.anchorMin = new Vector2(1, 1);
		btnRt.anchorMax = new Vector2(1, 1);
		btnRt.pivot = new Vector2(1, 1);
		btnRt.anchoredPosition = new Vector2(-80, -10);
		btnRt.sizeDelta = new Vector2(60, 60);

		TextMeshProUGUI btnText = CreateTMPChild(btnObj.transform, "Text", "Bag", 14);
		btnText.alignment = TextAlignmentOptions.Center;
		StretchFill(btnText.GetComponent<RectTransform>());

		// OnClick -> UIManager.ShowEquipment()
		UIManager uiMgr = Object.FindAnyObjectByType<UIManager>();
		if (uiMgr != null)
		{
			UnityEditor.Events.UnityEventTools.AddPersistentListener(
				btn.onClick,
				new UnityEngine.Events.UnityAction(uiMgr.ShowEquipment)
			);
			Debug.Log("[EquipmentSetup] EquipmentButton created and wired to ShowEquipment().");
		}
		else
		{
			Debug.LogWarning("[EquipmentSetup] UIManager not found. Wire OnClick manually.");
		}
	}

	/// <summary>
	/// Main Camera의 Culling Mask에서 EquipmentModel 레이어를 제외한다.
	/// </summary>
	private static void SetupMainCameraCulling()
	{
		Camera mainCam = Camera.main;
		if (mainCam == null)
		{
			Debug.LogWarning("[EquipmentSetup] Main Camera not found.");
			return;
		}

		int eqModelLayer = LayerMask.NameToLayer("EquipmentModel");
		if (eqModelLayer < 0)
		{
			Debug.LogWarning("[EquipmentSetup] EquipmentModel layer not found.");
			return;
		}

		mainCam.cullingMask &= ~(1 << eqModelLayer);
		EditorUtility.SetDirty(mainCam);
		Debug.Log("[EquipmentSetup] Main Camera culling mask updated - EquipmentModel excluded.");
	}

	// ============================
	// Helper Methods
	// ============================

	private static void WireModelCameraToPanel(Camera cam)
	{
		Canvas canvas = FindMainCanvas();
		if (canvas == null) return;
		Transform eqLayer = canvas.transform.Find("EquipmentLayer");
		if (eqLayer == null) return;
		UI_EquipmentPanel panel = eqLayer.GetComponent<UI_EquipmentPanel>();
		if (panel == null) return;

		SerializedObject so = new SerializedObject(panel);
		so.FindProperty("_modelCamera").objectReferenceValue = cam;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static void WireModelSpawnPointToPanel(Transform spawnPoint)
	{
		Canvas canvas = FindMainCanvas();
		if (canvas == null) return;
		Transform eqLayer = canvas.transform.Find("EquipmentLayer");
		if (eqLayer == null) return;
		UI_EquipmentPanel panel = eqLayer.GetComponent<UI_EquipmentPanel>();
		if (panel == null) return;

		SerializedObject so = new SerializedObject(panel);
		so.FindProperty("_modelSpawnPoint").objectReferenceValue = spawnPoint;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static Canvas FindMainCanvas()
	{
		Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
		foreach (var c in canvases)
		{
			if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
				return c;
		}
		// Fallback: any root canvas
		foreach (var c in canvases)
		{
			if (c.isRootCanvas) return c;
		}
		return null;
	}

	private static UI_EquipSlot CreateEquipSlot(Transform parent, string slotName, int slotTypeEnumValue)
	{
		GameObject slotObj = CreateUIObject($"EquipSlot_{slotName}", parent);
		RectTransform slotRt = slotObj.GetComponent<RectTransform>();
		slotRt.sizeDelta = new Vector2(80, 80);
		LayoutElement le = slotObj.AddComponent<LayoutElement>();
		le.preferredWidth = 80;
		le.preferredHeight = 80;

		slotObj.AddComponent<Button>();
		UI_EquipSlot slot = slotObj.AddComponent<UI_EquipSlot>();

		// Background (등급별 배경 이미지)
		GameObject bg = CreateUIObject("Background", slotObj.transform);
		Image bgImg = bg.AddComponent<Image>();
		bgImg.color = new Color(0.5f, 0.5f, 0.5f);
		StretchFill(bg.GetComponent<RectTransform>());

		// Icon
		GameObject icon = CreateUIObject("Icon", slotObj.transform);
		Image iconImg = icon.AddComponent<Image>();
		iconImg.enabled = false;
		RectTransform iconRt = icon.GetComponent<RectTransform>();
		iconRt.anchorMin = new Vector2(0.5f, 0.5f);
		iconRt.anchorMax = new Vector2(0.5f, 0.5f);
		iconRt.sizeDelta = new Vector2(64, 64);

		// LevelText
		GameObject lvObj = CreateUIObject("LevelText", slotObj.transform);
		TextMeshProUGUI lvText = lvObj.AddComponent<TextMeshProUGUI>();
		lvText.text = "Lv.1";
		lvText.fontSize = 12;
		lvText.alignment = TextAlignmentOptions.Bottom;
		RectTransform lvRt = lvObj.GetComponent<RectTransform>();
		lvRt.anchorMin = new Vector2(0, 0);
		lvRt.anchorMax = new Vector2(1, 0.25f);
		lvRt.offsetMin = Vector2.zero;
		lvRt.offsetMax = Vector2.zero;
		lvObj.SetActive(false);

		// EmptyPlaceholder
		GameObject emptyObj = CreateUIObject("EmptyPlaceholder", slotObj.transform);
		TextMeshProUGUI emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
		emptyText.text = slotName;
		emptyText.fontSize = 11;
		emptyText.alignment = TextAlignmentOptions.Center;
		emptyText.color = new Color(1, 1, 1, 0.3f);
		StretchFill(emptyObj.GetComponent<RectTransform>());

		// Wire fields
		SerializedObject so = new SerializedObject(slot);
		so.FindProperty("_bgImage").objectReferenceValue = bgImg;
		so.FindProperty("_iconImage").objectReferenceValue = iconImg;
		so.FindProperty("_levelText").objectReferenceValue = lvText;
		so.FindProperty("_slotType").enumValueIndex = slotTypeEnumValue;
		so.ApplyModifiedPropertiesWithoutUndo();

		return slot;
	}

	private static GameObject CreateUIObject(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	private static void StretchFill(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}

	private static TextMeshProUGUI CreateTMPChild(Transform parent, string name, string text, float fontSize)
	{
		GameObject obj = CreateUIObject(name, parent);
		TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = fontSize;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.color = Color.white;
		return tmp;
	}

	private static void SetAnchoredRect(TextMeshProUGUI tmp, float xMin, float yMin, float xMax, float yMax)
	{
		RectTransform rt = tmp.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(xMin, yMin);
		rt.anchorMax = new Vector2(xMax, yMax);
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}

	private static GameObject CreateButtonWithText(Transform parent, string name, string label,
		Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
	{
		GameObject obj = CreateUIObject(name, parent);
		RectTransform rt = obj.GetComponent<RectTransform>();
		rt.anchorMin = anchorMin;
		rt.anchorMax = anchorMax;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;

		Image img = obj.AddComponent<Image>();
		img.color = bgColor;
		obj.AddComponent<Button>();

		TextMeshProUGUI txt = CreateTMPChild(obj.transform, "Text", label, 16);
		txt.alignment = TextAlignmentOptions.Center;
		StretchFill(txt.GetComponent<RectTransform>());

		return obj;
	}
}
