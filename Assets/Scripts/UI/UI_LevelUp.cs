using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_LevelUp : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _titleText;
	[SerializeField] private TextMeshProUGUI _levelText;
	[SerializeField] private TextMeshProUGUI _subtitleText;
	[SerializeField] private UI_UpgradeSlot[] _slots;
	[SerializeField] private GameObject _panel;

	[Header("Roulette")]
	[SerializeField] private float _spinDuration = 2.0f;
	[SerializeField] private float _stopInterval = 0.5f;

	private Queue<int> _pendingLevelUps = new Queue<int>();
	private bool _isShowing = false;

	private void Start()
	{
		if (_panel != null)
			_panel.SetActive(false);

		ExpManager.Instance.OnLevelUp += OnLevelUp;

		for (int i = 0; i < _slots.Length; i++)
		{
			int idx = i;
			_slots[i].GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() => OnSlotSelected(idx));
		}
	}

	private void OnDestroy()
	{
		if (ExpManager.Instance != null)
			ExpManager.Instance.OnLevelUp -= OnLevelUp;
	}

	private void OnLevelUp(int level)
	{
		_pendingLevelUps.Enqueue(level);
		if (!_isShowing)
			Show(_pendingLevelUps.Dequeue());
	}

	private void Show(int level)
	{
		_isShowing = true;
		_panel.SetActive(true);
		UIManager.Instance.ShowLevelUp();
		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;

		if (_titleText != null) _titleText.text = "레벨 업그레이드!";
		if (_levelText != null) _levelText.text = $"레벨 {level}";
		if (_subtitleText != null) _subtitleText.text = "새로운 능력을 선택하세요!";

		StartCoroutine(RouletteCoroutine());
	}

	private IEnumerator RouletteCoroutine()
	{
		List<UpgradeInfo> picks = UpgradeDatabase.PickRandom(3);

		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].SetUpgrade(picks[i]);
			_slots[i].StartSpin();
		}

		yield return new WaitForSecondsRealtime(_spinDuration);

		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].StopSpin();
			if (i < _slots.Length - 1)
				yield return new WaitForSecondsRealtime(_stopInterval);
		}

		for (int i = 0; i < _slots.Length; i++)
			_slots[i].SetInteractable(true);
	}

	private void OnSlotSelected(int idx)
	{
		UpgradeInfo selected = _slots[idx].Assigned;
		PlayerController player = FindAnyObjectByType<PlayerController>();
		if (player != null)
		{
			PlayerUpgrade upgrade = player.GetComponent<PlayerUpgrade>();
			if (upgrade != null)
				upgrade.AddUpgrade(selected.type);
		}

		if (_pendingLevelUps.Count > 0)
			Show(_pendingLevelUps.Dequeue());
		else
			Close();
	}

	private void Close()
	{
		_isShowing = false;
		_panel.SetActive(false);
		UIManager.Instance.HideLevelUp();
		Time.timeScale = 1f;
		GameManager.Instance.IsPaused = false;
	}
}
