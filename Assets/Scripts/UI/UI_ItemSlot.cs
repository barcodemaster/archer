using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

/// <summary>
/// 인벤토리 목록의 아이템 슬롯 UI.
/// </summary>
public class UI_ItemSlot : MonoBehaviour
{
	[SerializeField] private Image _bgImage;
	[SerializeField] private Image _iconImage;
	[SerializeField] private TextMeshProUGUI _levelText;

	private EquipmentData _item;
	private UI_EquipmentPanel _equipmentPanel;
	private UI_GameOverPanel _gameOverPanel;

	public EquipmentData Item => _item;

	/// <summary>
	/// 아이템 정보를 표시한다.
	/// </summary>
	public void SetItem(EquipmentData item, EquipmentTable table, UI_EquipmentPanel panel)
	{
		_item = item;
		_equipmentPanel = panel;

		ApplyGradeBackground(table.grade);

		if (_iconImage != null)
		{
			Sprite sprite = IconHelper.GetSprite(table.icon);
			_iconImage.sprite = sprite;
			_iconImage.enabled = sprite != null;
		}

		if (_levelText != null)
			_levelText.text = $"Lv.{item.level}";

		Button btn = GetComponent<Button>();
		if (btn != null)
		{
			btn.onClick.RemoveAllListeners();
			btn.onClick.AddListener(OnClick);
		}
	}

	public void SetItem(EquipmentData item, EquipmentTable table, UI_GameOverPanel panel)
	{
		_item = item;
		_gameOverPanel = panel;

		ApplyGradeBackground(table.grade);

		if (_iconImage != null)
		{
			Sprite sprite = IconHelper.GetSprite(table.icon);
			_iconImage.sprite = sprite;
			_iconImage.enabled = sprite != null;
		}

		if (_levelText != null)
			_levelText.text = $"Lv.{item.level}";

		Button btn = GetComponent<Button>();
		if (btn != null)
		{
			btn.onClick.RemoveAllListeners();
			btn.onClick.AddListener(OnClick);
		}
	}

	public void SetItem(int count, Sprite icon, UI_GameOverPanel panel)
	{
		_bgImage.color = Color.gray;
		_iconImage.sprite = icon;
		_levelText.text = $"x{count}";
	}

	/// <summary>
	/// 등급별 배경 이미지를 적용한다.
	/// </summary>
	private void ApplyGradeBackground(EEquipGrade grade)
	{
		if (_bgImage != null)
		{
			_bgImage.sprite = GetGradeSprite(grade);
			_bgImage.color = Color.white;
		}
	}

	private void OnClick()
	{
		if (_equipmentPanel != null)
			_equipmentPanel.OnItemSlotClicked(_item);
	}
}
