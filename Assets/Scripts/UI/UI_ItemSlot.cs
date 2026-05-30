using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

/// <summary>
/// 인벤토리 목록의 아이템 슬롯 UI.
/// </summary>
public class UI_ItemSlot : MonoBehaviour
{
	[SerializeField] private Image _backgroundImage;
	[SerializeField] private Image _iconImage;
	[SerializeField] private TextMeshProUGUI _levelText;

	private EquipmentData _item;
	private UI_EquipmentPanel _panel;

	public EquipmentData Item => _item;

	/// <summary>
	/// 아이템 정보를 표시한다.
	/// </summary>
	public void SetItem(EquipmentData item, EquipmentTable table, UI_EquipmentPanel panel)
	{
		_item = item;
		_panel = panel;

		if (_backgroundImage != null)
			_backgroundImage.color = GetGradeColor(table.grade);

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

	private void OnClick()
	{
		if (_panel != null)
			_panel.OnItemSlotClicked(_item);
	}
}
