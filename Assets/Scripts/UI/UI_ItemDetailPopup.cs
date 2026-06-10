using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

/// <summary>
/// 아이템 상세 정보 팝업 UI.
/// </summary>
public class UI_ItemDetailPopup : MonoBehaviour
{
	[Header("Info")]
	[SerializeField] private Image _iconImage;
	[SerializeField] private Image _gradeBg;
	[SerializeField] private TextMeshProUGUI _nameText;
	[SerializeField] private TextMeshProUGUI _gradeText;
	[SerializeField] private TextMeshProUGUI _levelText;
	[SerializeField] private TextMeshProUGUI _statsText;
	[SerializeField] private TextMeshProUGUI _descriptionText;
	[SerializeField] private TextMeshProUGUI _costText;

	[Header("Buttons")]
	[SerializeField] private Button _equipButton;
	[SerializeField] private TextMeshProUGUI _equipButtonText;
	[SerializeField] private Button _levelUpButton;
	[SerializeField] private Button _closeButton;

	private EquipmentData _currentItem;
	private EEquipSlot _currentSlot;
	private bool _isEquipped;
	private UI_EquipmentPanel _panel;

	private void Awake()
	{
		_panel = GetComponentInParent<UI_EquipmentPanel>();

		if (_equipButton != null)
			_equipButton.onClick.AddListener(OnEquipClicked);
		if (_levelUpButton != null)
			_levelUpButton.onClick.AddListener(OnLevelUpClicked);
		if (_closeButton != null)
			_closeButton.onClick.AddListener(() => gameObject.SetActive(false));
	}

	/// <summary>
	/// 장착 중인 아이템의 상세 정보를 표시한다.
	/// </summary>
	public void ShowEquipped(EquipmentData item, EEquipSlot slot)
	{
		_currentItem = item;
		_currentSlot = slot;
		_isEquipped = true;

		RefreshDisplay();

		if (_equipButtonText != null)
			_equipButtonText.text = "Unequip";

		gameObject.SetActive(true);
	}

	/// <summary>
	/// 미장착 아이템의 상세 정보를 표시한다.
	/// </summary>
	public void ShowUnequipped(EquipmentData item)
	{
		_currentItem = item;
		_isEquipped = false;

		RefreshDisplay();

		if (_equipButtonText != null)
			_equipButtonText.text = "Equip";

		gameObject.SetActive(true);
	}

	private void RefreshDisplay()
	{
		EquipmentTable table = EquipmentDatabase.GetById(_currentItem.tableId);
		if (table == null) return;

		if (_gradeBg != null)
		{
			_gradeBg.sprite = GetGradeSprite(table.grade);
			_gradeBg.color = Color.white;
		}

		if (_iconImage != null)
		{
			Sprite sprite = IconHelper.GetSprite(table.icon);
			_iconImage.sprite = sprite;
			_iconImage.enabled = sprite != null;
		}

		if (_nameText != null)
			_nameText.text = table.name;

		if (_gradeText != null)
		{
			_gradeText.text = table.grade.ToString();
			_gradeText.color = GetGradeColor(table.grade);
		}

		if (_levelText != null)
			_levelText.text = $"레벨: {_currentItem.level}/50";

		if (_statsText != null)
		{
			string mainStatName = table.mainStatType == EMainStatType.Attack ? "ATK" : "Max HP";
			string mainStatStr = $"{mainStatName} +{_currentItem.GetMainStat(table):F0}";
			if (_currentItem.level < 50)
				mainStatStr += $" (<color=#00FF00>+{table.mainStatPerLevel:F0}</color>)";
			_statsText.text = $"{mainStatStr}\n{_currentItem.subStatType} +{_currentItem.subStatValue:F1}%";
		}

		if (_descriptionText != null)
			_descriptionText.text = table.description;

		int cost = _currentItem.GetLevelUpCost(table);
		if (_costText != null)
			_costText.text = $"레벨업\nx{cost}";

		if (_levelUpButton != null)
		{
			bool canAfford = GoldManager.Instance.Gold >= cost;
			bool canLevel = _currentItem.level < 50;
			_levelUpButton.interactable = canAfford && canLevel;
		}
	}

	private void OnEquipClicked()
	{
		if (_currentItem == null) return;

		if (_isEquipped)
		{
			EquipmentManager.Instance.Unequip(_currentSlot);
		}
		else
		{
			EEquipSlot slot = EquipmentManager.Instance.FindAvailableSlot(_currentItem);
			EquipmentManager.Instance.Equip(_currentItem, slot);
		}

		// 플레이어 HP 재계산
		PlayerController player = PlayerController.Instance;
		if (player != null) player.RecalculateMaxHp();

		gameObject.SetActive(false);
		if (_panel != null) _panel.Refresh();
	}

	private void OnLevelUpClicked()
	{
		if (_currentItem == null) return;

		if (EquipmentManager.Instance.LevelUp(_currentItem))
		{
			// 장착 중이면 HP 재계산
			if (_currentItem.isEquipped)
			{
				PlayerController player = PlayerController.Instance;
				if (player != null) player.RecalculateMaxHp();
			}

			RefreshDisplay();
			if (_panel != null) _panel.Refresh();
		}
	}
}
