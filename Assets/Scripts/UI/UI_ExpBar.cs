using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ExpBar : MonoBehaviour
{
	[SerializeField] private Slider _slider;
	[SerializeField] private TextMeshProUGUI _levelText;

	private void Start()
	{
		ExpManager.Instance.OnExpChanged += UpdateBar;
		UpdateBar(0, ExpManager.Instance.ExpToNextLevel);
		UpdateLevelText();
		ExpManager.Instance.OnLevelUp += OnLevelUp;
	}

	private void OnDestroy()
	{
		if (ExpManager.Instance != null)
		{
			ExpManager.Instance.OnExpChanged -= UpdateBar;
			ExpManager.Instance.OnLevelUp -= OnLevelUp;
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
