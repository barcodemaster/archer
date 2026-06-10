using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_AlivePanel : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _timeText;
	[SerializeField] private TextMeshProUGUI _costText;
	[SerializeField] private TextMeshProUGUI _aliveText;
	[SerializeField] private Button _continueButton;
	[SerializeField] private Image _goldImage;
	

	private int _delayTime = 5;
	private float _elapsedTime = 0f;
	private float _totalTime = 0f;


	public void Show()
	{
		_delayTime = 5;
		_elapsedTime = 0f;
		_totalTime = 0f;

		_continueButton.onClick.RemoveAllListeners();
		_continueButton.onClick.AddListener(OnReviveClicked);
		_timeText.text = $"{_delayTime}";

		int cost = StageManager.Instance.AliveCost;
		_costText.text = $"{cost}";
		_aliveText.text = "부활";
		_goldImage.sprite = IconHelper.GetSprite("Gold");

		_continueButton.interactable = GoldManager.Instance.Gold >= cost;

		GameManager.Instance.IsPaused = true;
		Time.timeScale = 0f;
	}

	private void Update()
	{
		_elapsedTime += Time.unscaledDeltaTime;
		if(_elapsedTime >= 1f)
		{
			_delayTime--;
			if (_delayTime <= 0)
			{
				Time.timeScale = 1f;
				int stageIndex = StageManager.Instance.CurrentStageIndex;
				int gold = GoldManager.Instance.Gold;
				UIManager.Instance.ShowGameOver(stageIndex,gold);
				gameObject.SetActive(false);
				return;
			}
			_timeText.text = $"{_delayTime}";
			_totalTime += 1f;
			_elapsedTime = 0f;
		}
	}

	private void OnReviveClicked()
	{
		int cost = StageManager.Instance.AliveCost;
		if (!GoldManager.Instance.SpendGold(cost)) return;

		Time.timeScale = 1f;
		gameObject.SetActive(false);
		GameManager.Instance.IsPaused = false;
		GameManager.Instance.RevivePlayer();
	}
}
