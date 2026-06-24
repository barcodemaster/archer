using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

/// <summary>
/// 장비/펫 아이템을 브라우징하고 즉시 획득할 수 있는 디버그 치트 패널.
/// </summary>
public class UI_CheatPanel : MonoBehaviour
{
	private ScrollRect _scrollRect;
	private RectTransform _contentRoot;

	/// <summary>
	/// 패널을 열고 아이템 목록을 갱신한다.
	/// </summary>
	public void Open()
	{
		gameObject.SetActive(true);
		Refresh();
	}

	/// <summary>
	/// 패널을 닫는다.
	/// </summary>
	public void Close()
	{
		gameObject.SetActive(false);
	}

	private void Awake()
	{
		BuildUI();
	}

	/// <summary>
	/// 전체 UI 계층 구조를 코드로 동적 생성한다.
	/// </summary>
	private void BuildUI()
	{
		RectTransform root = GetComponent<RectTransform>();
		root.anchorMin = Vector2.zero;
		root.anchorMax = Vector2.one;
		root.offsetMin = Vector2.zero;
		root.offsetMax = Vector2.zero;

		// 반투명 검정 배경
		Image bg = gameObject.AddComponent<Image>();
		bg.color = new Color(0f, 0f, 0f, 0.85f);
		bg.raycastTarget = true;

		// 헤더 영역
		GameObject headerObj = CreateRectObj("Header", transform);
		RectTransform headerRT = headerObj.GetComponent<RectTransform>();
		headerRT.anchorMin = new Vector2(0f, 1f);
		headerRT.anchorMax = new Vector2(1f, 1f);
		headerRT.pivot = new Vector2(0.5f, 1f);
		headerRT.sizeDelta = new Vector2(0f, 80f);

		// 타이틀
		GameObject titleObj = CreateTextObj("Title", headerObj.transform, "Cheat: Items", 32f, Color.white);
		RectTransform titleRT = titleObj.GetComponent<RectTransform>();
		titleRT.anchorMin = new Vector2(0f, 0f);
		titleRT.anchorMax = new Vector2(1f, 1f);
		titleRT.offsetMin = new Vector2(20f, 0f);
		titleRT.offsetMax = new Vector2(-80f, 0f);
		titleObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

		// 닫기 버튼
		GameObject closeBtnObj = CreateRectObj("CloseBtn", headerObj.transform);
		RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
		closeBtnRT.anchorMin = new Vector2(1f, 0.5f);
		closeBtnRT.anchorMax = new Vector2(1f, 0.5f);
		closeBtnRT.pivot = new Vector2(1f, 0.5f);
		closeBtnRT.sizeDelta = new Vector2(60f, 60f);
		closeBtnRT.anchoredPosition = new Vector2(-10f, 0f);

		Image closeBg = closeBtnObj.AddComponent<Image>();
		closeBg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
		Button closeBtn = closeBtnObj.AddComponent<Button>();
		closeBtn.targetGraphic = closeBg;
		closeBtn.onClick.AddListener(Close);

		GameObject closeLabel = CreateTextObj("X", closeBtnObj.transform, "X", 28f, Color.white);
		RectTransform closeLabelRT = closeLabel.GetComponent<RectTransform>();
		closeLabelRT.anchorMin = Vector2.zero;
		closeLabelRT.anchorMax = Vector2.one;
		closeLabelRT.offsetMin = Vector2.zero;
		closeLabelRT.offsetMax = Vector2.zero;
		closeLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

		// ScrollRect 영역
		GameObject scrollObj = CreateRectObj("Scroll", transform);
		RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
		scrollRT.anchorMin = Vector2.zero;
		scrollRT.anchorMax = Vector2.one;
		scrollRT.offsetMin = new Vector2(10f, 10f);
		scrollRT.offsetMax = new Vector2(-10f, -80f);

		_scrollRect = scrollObj.AddComponent<ScrollRect>();
		_scrollRect.horizontal = false;
		_scrollRect.vertical = true;
		_scrollRect.movementType = ScrollRect.MovementType.Clamped;

		// Viewport + Mask
		GameObject viewportObj = CreateRectObj("Viewport", scrollObj.transform);
		RectTransform viewportRT = viewportObj.GetComponent<RectTransform>();
		viewportRT.anchorMin = Vector2.zero;
		viewportRT.anchorMax = Vector2.one;
		viewportRT.offsetMin = Vector2.zero;
		viewportRT.offsetMax = Vector2.zero;
		viewportObj.AddComponent<RectMask2D>();

		// Content
		GameObject contentObj = CreateRectObj("Content", viewportObj.transform);
		_contentRoot = contentObj.GetComponent<RectTransform>();
		_contentRoot.anchorMin = new Vector2(0f, 1f);
		_contentRoot.anchorMax = new Vector2(1f, 1f);
		_contentRoot.pivot = new Vector2(0.5f, 1f);
		_contentRoot.sizeDelta = new Vector2(0f, 0f);

		VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(8, 8, 8, 8);
		vlg.spacing = 6f;
		vlg.childAlignment = TextAnchor.UpperCenter;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
		csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		_scrollRect.viewport = viewportRT;
		_scrollRect.content = _contentRoot;
	}

	/// <summary>
	/// 아이템 목록을 갱신한다.
	/// </summary>
	private void Refresh()
	{
		// 기존 행 제거
		for (int i = _contentRoot.childCount - 1; i >= 0; i--)
			Destroy(_contentRoot.GetChild(i).gameObject);

		EquipmentTable[] all = EquipmentDatabase.GetAll();
		Debug.Log($"[CheatPanel] items={all.Length}, contentChildren={_contentRoot.childCount}");
		foreach (var table in all)
			CreateRow(table);

		LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
		Canvas.ForceUpdateCanvases();
	}

	/// <summary>
	/// 아이템 행 하나를 생성한다.
	/// </summary>
	private void CreateRow(EquipmentTable table)
	{
		GameObject rowObj = CreateRectObj("Row_" + table.id, _contentRoot);
		Image rowBg = rowObj.AddComponent<Image>();
		rowBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

		HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
		hlg.padding = new RectOffset(10, 10, 6, 6);
		hlg.spacing = 8f;
		hlg.childAlignment = TextAnchor.MiddleLeft;
		hlg.childControlWidth = true;
		hlg.childControlHeight = true;
		hlg.childForceExpandWidth = false;
		hlg.childForceExpandHeight = true;

		LayoutElement rowLE = rowObj.AddComponent<LayoutElement>();
		rowLE.preferredHeight = 60f;

		// 이름 (등급 색상)
		Color gradeColor = GetGradeColor(table.grade);
		GameObject nameObj = CreateTextObj("Name", rowObj.transform,
			$"[{table.grade}] {table.name}", 20f, gradeColor);
		LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
		nameLE.flexibleWidth = 3f;

		// 슬롯
		string slotName = GetSlotDisplayName(table.slot);
		GameObject slotObj = CreateTextObj("Slot", rowObj.transform, slotName, 18f,
			new Color(0.7f, 0.7f, 0.7f));
		LayoutElement slotLE = slotObj.AddComponent<LayoutElement>();
		slotLE.flexibleWidth = 1f;

		// 메인 스탯
		string statText = $"{table.mainStatType} +{table.baseMainStat}";
		GameObject statObj = CreateTextObj("Stat", rowObj.transform, statText, 18f,
			new Color(0.9f, 0.9f, 0.6f));
		LayoutElement statLE = statObj.AddComponent<LayoutElement>();
		statLE.flexibleWidth = 1.5f;

		// [획득] 버튼
		GameObject btnObj = CreateRectObj("AddBtn", rowObj.transform);
		Image btnBg = btnObj.AddComponent<Image>();
		btnBg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
		Button btn = btnObj.AddComponent<Button>();
		btn.targetGraphic = btnBg;

		LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
		btnLE.preferredWidth = 80f;
		btnLE.flexibleWidth = 0f;

		GameObject btnLabel = CreateTextObj("Label", btnObj.transform, "획득", 20f, Color.white);
		RectTransform btnLabelRT = btnLabel.GetComponent<RectTransform>();
		btnLabelRT.anchorMin = Vector2.zero;
		btnLabelRT.anchorMax = Vector2.one;
		btnLabelRT.offsetMin = Vector2.zero;
		btnLabelRT.offsetMax = Vector2.zero;
		btnLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

		EquipmentTable captured = table;
		btn.onClick.AddListener(() => OnAddClicked(captured));
	}

	/// <summary>
	/// 획득 버튼 클릭 시 아이템을 생성하여 인벤토리에 추가한다.
	/// </summary>
	private void OnAddClicked(EquipmentTable table)
	{
		ESubStatType[] subTypes = (ESubStatType[])Enum.GetValues(typeof(ESubStatType));
		ESubStatType subType = subTypes[UnityEngine.Random.Range(0, subTypes.Length)];
		float subValue = UnityEngine.Random.Range(table.baseSubMin, table.baseSubMax);

		EquipmentData item = new EquipmentData
		{
			uid = Guid.NewGuid().ToString(),
			tableId = table.id,
			level = 1,
			subStatType = subType,
			subStatValue = Mathf.Round(subValue * 10f) / 10f,
			isEquipped = false,
		};

		EquipmentManager.Instance.AddItem(item);
	}

	/// <summary>
	/// RectTransform만 가진 빈 GameObject를 생성한다.
	/// </summary>
	private static GameObject CreateRectObj(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	/// <summary>
	/// TextMeshProUGUI를 가진 GameObject를 생성한다.
	/// </summary>
	private static GameObject CreateTextObj(string name, Transform parent, string text,
		float fontSize, Color color)
	{
		GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
		go.transform.SetParent(parent, false);
		TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = fontSize;
		tmp.color = color;
		tmp.enableWordWrapping = false;
		tmp.overflowMode = TextOverflowModes.Ellipsis;
		tmp.alignment = TextAlignmentOptions.MidlineLeft;
		return go;
	}
}
