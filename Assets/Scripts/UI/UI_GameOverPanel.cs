using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임오버 패널. 도달 스테이지와 획득 골드를 표시하고 재시작 버튼을 제공한다.
/// </summary>
public class UI_GameOverPanel : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _titleText;
	[SerializeField] private TextMeshProUGUI _stageLabelText;
	[SerializeField] private TextMeshProUGUI _stageText;
	[SerializeField] private Button _restartButton;
	[SerializeField] private Transform _content;
	[SerializeField] private GameObject _itemSlotPrefab;

	private List<GameObject> _spawnedSlots = new List<GameObject>();

	private void Awake()
	{
		if (_restartButton != null)
			_restartButton.onClick.AddListener(OnRestartClicked);

		SetupScrollView();
	}

	/// <summary>
	/// 게임오버 정보를 설정하고 패널을 표시한다.
	/// </summary>
	public void Show(int stageIndex, int gold)
	{
		gameObject.SetActive(true);

		_titleText.text = "게임 종료";
		_stageLabelText.text = "스테이지";
		_stageText.text = $"{StageManager.Instance.CurrentStageIndex + 1}";

		RefreshList();
		AdjustGridCellSize();

		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;
	}

	private void OnRestartClicked()
	{
		Time.timeScale = 1f;
		GameManager.Instance.IsPaused = false;
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	private void RefreshList()
	{
		foreach (var go in _spawnedSlots)
			if (go != null) Destroy(go);
		_spawnedSlots.Clear();

		if (_content == null || _itemSlotPrefab == null) return;

		for (int i = _content.childCount - 1; i >= 0; i--)
			Destroy(_content.GetChild(i).gameObject);

		int collectedGold = StageManager.Instance.GetTotalGoldCollected();

		GameObject goldGo = Instantiate(_itemSlotPrefab, _content);
		UI_ItemSlot goldSlot = goldGo.GetComponent<UI_ItemSlot>();
		Sprite goldIcon = IconHelper.GetSprite("Gold");
		if (goldSlot != null)
			goldSlot.SetItem(collectedGold, goldIcon, this);

		var itemList = StageManager.Instance.GetCollectedEquipments();

		foreach (var item in itemList)
		{
			if (item.isEquipped) continue;

			EquipmentTable table = EquipmentDatabase.GetById(item.tableId);
			if (table == null) continue;

			GameObject go = Instantiate(_itemSlotPrefab, _content);
			UI_ItemSlot slot = go.GetComponent<UI_ItemSlot>();
			if (slot != null)
				slot.SetItem(item, table, this);
			_spawnedSlots.Add(go);
		}
	}

	private void AdjustGridCellSize()
	{
		if (_content == null) return;

		GridLayoutGroup grid = _content.GetComponent<GridLayoutGroup>();
		if (grid == null) return;

		ScrollRect scrollRect = _content.GetComponentInParent<ScrollRect>();
		if (scrollRect == null || scrollRect.viewport == null) return;

		float width = scrollRect.viewport.rect.width;
		if (width <= 0) return;

		int columns = 5;
		float totalSpacing = grid.spacing.x * (columns - 1) + grid.padding.left + grid.padding.right;
		float cellWidth = (width - totalSpacing) / columns;
		grid.cellSize = new Vector2(cellWidth, cellWidth);
	}

	private void SetupScrollView()
	{
		if (_content == null) return;

		ScrollRect scrollRect = _content.GetComponentInParent<ScrollRect>();
		if (scrollRect == null) return;

		// 세로 스크롤만 허용
		scrollRect.vertical = true;
		scrollRect.horizontal = false;
		scrollRect.movementType = ScrollRect.MovementType.Elastic; // 스크롤이 끝에서 튕기는 효과
		scrollRect.scrollSensitivity = 30f;

		// 스크롤바 제거
		if (scrollRect.verticalScrollbar != null)
		{
			scrollRect.verticalScrollbar.gameObject.SetActive(false);
			scrollRect.verticalScrollbar = null;
		}

		// Content RectTransform: top-stretch, pivot 상단
		// 부모기준 위쪽 전체 Stretch, viewport폭 = Content폭
		RectTransform contentRect = _content.GetComponent<RectTransform>();
		if (contentRect != null)
		{
			contentRect.anchorMin = new Vector2(0f, 1f);
			contentRect.anchorMax = new Vector2(1f, 1f);
			contentRect.pivot = new Vector2(0.5f, 1f); // 아이템이 세로로 추가되면 아래 방향으로 커져야하기때문에 기준점을 상단 중앙으로
			contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
			contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);
		}

		// GridLayoutGroup 설정 (없으면 추가)
		GridLayoutGroup grid = _content.GetComponent<GridLayoutGroup>();
		if (grid == null)
			grid = _content.gameObject.AddComponent<GridLayoutGroup>();

		grid.cellSize = new Vector2(120f, 120f);
		grid.spacing = new Vector2(10f, 10f);
		grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
		grid.startAxis = GridLayoutGroup.Axis.Horizontal;
		grid.childAlignment = TextAnchor.UpperLeft;
		grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		grid.constraintCount = 5;

		// ContentSizeFitter로 스크롤 가능하게
		ContentSizeFitter fitter = _content.GetComponent<ContentSizeFitter>();
		if (fitter == null)
			fitter = _content.gameObject.AddComponent<ContentSizeFitter>();

		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		RectTransform viewportRt = scrollRect.viewport;
		if (viewportRt != null && viewportRt.GetComponent<RectMask2D>() == null)
			viewportRt.gameObject.AddComponent<RectMask2D>();
	}
}
