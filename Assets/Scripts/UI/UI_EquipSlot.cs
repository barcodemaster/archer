using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

/// <summary>
/// 장비창의 장착 슬롯 UI.
/// </summary>
public class UI_EquipSlot : MonoBehaviour
{
	[SerializeField] private Image _bgImage;
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

		ApplyGradeBackground(table.grade);

		if (_iconImage != null)
		{
			Sprite sprite = IconHelper.GetSprite(table.icon);
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

		if (_bgImage != null)
		{
			_bgImage.sprite = GetGradeSprite(EEquipGrade.Common);
			_bgImage.color = new Color(0.5f, 0.5f, 0.5f);
		}

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
		if (_panel == null)
			_panel = GetComponentInParent<UI_EquipmentPanel>();

		if (_panel != null)
			_panel.OnEquipSlotClicked(this);
	}
}
