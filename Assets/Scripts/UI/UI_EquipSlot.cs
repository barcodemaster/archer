using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

/// <summary>
/// 장비창의 장착 슬롯 UI.
/// </summary>
public class UI_EquipSlot : MonoBehaviour
{
	[SerializeField] private Image _backgroundImage;
	[SerializeField] private Image _iconImage;
	[SerializeField] private TextMeshProUGUI _levelText;
	[SerializeField] private RawImage _emptyPlaceholderImage;
	[SerializeField] private Texture2D _emptyPlaceholderTexture;
	[SerializeField] private EEquipSlot _slotType;

	private EquipmentData _item;
	private UI_EquipmentPanel _panel;

	public EEquipSlot SlotType => _slotType;
	public EquipmentData Item => _item;

	private void Awake()
	{
		_panel = GetComponentInParent<UI_EquipmentPanel>();
		Button btn = GetComponent<Button>();
		if (btn != null)
			btn.onClick.AddListener(OnClick);
	}

	/// <summary>
	/// 장착된 아이템을 표시한다.
	/// </summary>
	public void SetItem(EquipmentData item, EquipmentTable table)
	{
		_item = item;

		if (_backgroundImage != null)
			_backgroundImage.color = GetGradeColor(table.grade);

		if (_iconImage != null)
		{
			IconAtlas atlas = Resources.Load<IconAtlas>("Data/IconAtlas");
			Sprite sprite = atlas != null ? atlas.GetSprite(table.icon) : null;
			_iconImage.sprite = sprite;
			_iconImage.enabled = sprite != null;
		}

		if (_levelText != null)
		{
			_levelText.text = $"Lv.{item.level}";
			_levelText.gameObject.SetActive(true);
		}

		if (_emptyPlaceholderImage != null)
			_emptyPlaceholderImage.enabled = false;
	}

	/// <summary>
	/// 빈 슬롯 상태로 초기화한다.
	/// </summary>
	public void SetEmpty()
	{
		_item = null;

		if (_backgroundImage != null)
			_backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

		if (_iconImage != null)
			_iconImage.enabled = false;

		if (_levelText != null)
			_levelText.gameObject.SetActive(false);

		if (_emptyPlaceholderImage != null)
		{
			_emptyPlaceholderImage.texture = _emptyPlaceholderTexture;
			_emptyPlaceholderImage.enabled = true;
		}
	}

	private void OnClick()
	{
		if (_panel != null)
			_panel.OnEquipSlotClicked(this);
	}
}
