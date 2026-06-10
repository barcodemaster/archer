using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ExpBar : MonoBehaviour
{
	[SerializeField] private Slider _slider;
	[SerializeField] private TextMeshProUGUI _levelText;

	private ExpManager _expManager;

	private void Start()
	{
		_expManager = ExpManager.Instance;
		_expManager.OnExpChanged += UpdateBar;
		UpdateBar(0, _expManager.ExpToNextLevel);
		UpdateLevelText();
		_expManager.OnLevelUp += OnLevelUp;
	}

	private void OnDestroy()
	{
		if (_expManager != null)
		{
			_expManager.OnExpChanged -= UpdateBar;
			_expManager.OnLevelUp -= OnLevelUp;
		}
	}

	private void UpdateBar(int current, int max)
	{
		if (_slider != null)
			_slider.value = (float)current / max;
	}

	private void OnLevelUp(int level)
	{
		UpdateLevelText();
	}

	private void UpdateLevelText()
	{
		if (_levelText != null)
			_levelText.text = $"Lv.{ExpManager.Instance.Level}";
	}
}
