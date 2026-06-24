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
	[SerializeField] private UI_SkillNotify _skillNotify;

	[Header("Roulette")]
	[SerializeField] private float _spinDuration = 1f;
	[SerializeField] private float _stopInterval = 0.5f;
	[SerializeField] private float _delayShowPanel = 1.5f;

private Queue<int> _pendingLevelUps = new Queue<int>();
	private bool _isShowing = false;
	private bool _isIntro = false;

	private int _tapCount;
	private bool _isAnimating;
	private Coroutine _rouletteCoroutine;
	private ExpManager _expManager;

	private void Start()
	{
		if (_panel != null)
		{
			_panel.SetActive(false);

			// 패널 탭 감지용 Button 등록
			UnityEngine.UI.Button panelBtn = _panel.GetComponent<UnityEngine.UI.Button>();
			if (panelBtn == null)
				panelBtn = _panel.AddComponent<UnityEngine.UI.Button>();
			panelBtn.transition = UnityEngine.UI.Selectable.Transition.None;
			panelBtn.onClick.AddListener(OnPanelTapped);
		}

		_expManager = ExpManager.Instance;
		_expManager.OnLevelUp += OnLevelUp;

		for (int i = 0; i < _slots.Length; i++)
			_slots[i].Init(i, OnSlotSelected);
	}

	private void OnDestroy()
	{
		if (_expManager != null)
			_expManager.OnLevelUp -= OnLevelUp;
	}

	/// <summary>
	/// 인트로 스테이지에서 레벨업 없이 능력 룰렛을 직접 연다.
	/// </summary>
	public void ShowIntro()
	{
		gameObject.SetActive(true);
		_isShowing = true;
		_isIntro = true;
		UIManager.Instance.ShowLevelUp();
		_panel.SetActive(true);
		AudioManager.Instance?.PlayUpgradePanelOpen();
		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;
		UIManager.Instance.ResetEventSystem();

		if (_titleText != null) _titleText.text = "능력을 선택하세요!";
		if (_levelText != null) _levelText.text = "";
		if (_subtitleText != null) _subtitleText.text = "모험을 시작할 능력을 골라보세요!";

		List<UpgradeInfo> picks = UpgradeDatabase.PickRandom(3);
		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].SetUpgrade(picks[i]);
			_slots[i].StartSpin();
		}

		_tapCount = 0;
		_isAnimating = true;
		_rouletteCoroutine = StartCoroutine(RouletteStopCoroutine());
	}

	private void OnLevelUp(int level)
	{
		_pendingLevelUps.Enqueue(level);
		if (!_isShowing)
		{
			gameObject.SetActive(true);
			StartCoroutine(ShowDelay());
		}
	}

	private IEnumerator ShowDelay()
	{
		yield return new WaitForSeconds(_delayShowPanel);

		Show(_pendingLevelUps.Dequeue());
	}

	private void Show(int level)
	{
		_isShowing = true;
		UIManager.Instance.ShowLevelUp();
		_panel.SetActive(true);
		AudioManager.Instance?.PlayUpgradePanelOpen();
		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;

		// 패널이 열리는 시점에 EventSystem 리셋 (마우스 pressed/dragged 상태 제거)
		UIManager.Instance.ResetEventSystem();

		if (_titleText != null) _titleText.text = "레벨 업그레이드!";
		if (_levelText != null) _levelText.text = $"레벨 {level}";
		if (_subtitleText != null) _subtitleText.text = "새로운 능력을 선택하세요!";

		// 슬롯 세팅 + 스핀을 Show()에서 직접 시작 (프레임 딜레이 제거)
		List<UpgradeInfo> picks = UpgradeDatabase.PickRandom(3);
		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].SetUpgrade(picks[i]);
			_slots[i].StartSpin();
		}

		_tapCount = 0;
		_isAnimating = true;
		_rouletteCoroutine = StartCoroutine(RouletteStopCoroutine());
	}

	/// <summary>
	/// 패널 탭 시 호출. 애니메이션 중 2회 이상 탭하면 스킵한다.
	/// </summary>
	public void OnPanelTapped()
	{
		if (!_isAnimating) return;
		_tapCount++;
		if (_tapCount >= 2)
			SkipAnimation();
	}

	/// <summary>
	/// 룰렛 애니메이션을 즉시 종료하고 최종 결과를 표시한다.
	/// </summary>
	private void SkipAnimation()
	{
		if (_rouletteCoroutine != null)
		{
			StopCoroutine(_rouletteCoroutine);
			_rouletteCoroutine = null;
		}
		_isAnimating = false;
		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].StopSpin(false);
			_slots[i].SetInteractable(true);
		}
		AudioManager.Instance?.PlayUpgradeSlotConfirm();
	}

	private IEnumerator RouletteStopCoroutine()
	{
		yield return new WaitForSecondsRealtime(_spinDuration);

		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].StopSpin();
			if (i < _slots.Length - 1)
				yield return new WaitForSecondsRealtime(_stopInterval);
		}

		for (int i = 0; i < _slots.Length; i++)
			_slots[i].SetInteractable(true);

		_isAnimating = false;
		_rouletteCoroutine = null;
	}

	private void OnSlotSelected(int idx)
	{
		// 중복 선택 방지
		for (int i = 0; i < _slots.Length; i++)
			_slots[i].SetInteractable(false);

		// 업그레이드 적용
		AchievementManager.Instance.AddProgress(Define.EAchievementType.UpgradeSelected);
		UpgradeInfo selected = _slots[idx].Assigned;
		PlayerController player = PlayerController.Instance;
		if (player != null)
		{
			PlayerUpgrade upgrade = player.GetComponent<PlayerUpgrade>();
			if (upgrade != null)
				upgrade.AddUpgrade(selected.type);
			player.ApplyUpgradeEffect(selected.type);
		}

		// 알림 패널 표시
		if (_skillNotify != null)
			_skillNotify.Show(selected.name, selected.description);

		if (_pendingLevelUps.Count > 0)
			Show(_pendingLevelUps.Dequeue());
		else
			Close();
	}

	private void Close()
	{
		bool wasIntro = _isIntro;
		_isShowing = false;
		_isIntro = false;
		_panel.SetActive(false);
		UIManager.Instance.HideLevelUp();
		Time.timeScale = 1f;
		GameManager.Instance.IsPaused = false;

		if (wasIntro)
			StageManager.Instance.OnIntroCompleted();
	}
}
