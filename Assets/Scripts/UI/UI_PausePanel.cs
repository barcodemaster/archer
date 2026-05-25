using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Define;

/// <summary>
/// 일시정지 버튼 + 습득 스킬 표시 패널.
/// </summary>
public class UI_PausePanel : MonoBehaviour
{
	[SerializeField] private GameObject _panel;
	[SerializeField] private Button _pauseButton;
	[SerializeField] private Button _resumeButton;
	[SerializeField] private Transform _skillIconContainer;
	[SerializeField] private GameObject _skillIconPrefab;
	[SerializeField] private float _iconAnimInterval = 0.1f;
	[SerializeField] private float _panelFadeDuration = 0.3f;

	private CanvasGroup _canvasGroup;

	private void Start()
	{
		if (_panel != null)
		{
			_panel.SetActive(false);
			_canvasGroup = _panel.GetComponent<CanvasGroup>();
			if (_canvasGroup == null)
				_canvasGroup = _panel.AddComponent<CanvasGroup>();
		}

		if (_pauseButton != null)
			_pauseButton.onClick.AddListener(OnPauseClicked);
		if (_resumeButton != null)
			_resumeButton.onClick.AddListener(OnResumeClicked);
	}

	/// <summary>
	/// 일시정지 버튼 클릭 시 호출.
	/// </summary>
	public void OnPauseClicked()
	{
		if (GameManager.Instance.IsPaused) return;

		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;

		_panel.SetActive(true);
		_canvasGroup.alpha = 1f;
		UIManager.Instance.ShowPause();

		// 기존 아이콘 제거
		for (int i = _skillIconContainer.childCount - 1; i >= 0; i--)
			Destroy(_skillIconContainer.GetChild(i).gameObject);

		// 습득한 스킬 표시
		PlayerUpgrade playerUpgrade = FindAnyObjectByType<PlayerUpgrade>();
		if (playerUpgrade != null)
		{
			UpgradeInfo[] allUpgrades = UpgradeDatabase.GetAll();
			foreach (var info in allUpgrades)
			{
				if (playerUpgrade.GetLevel(info.type) > 0)
				{
					GameObject iconObj = Instantiate(_skillIconPrefab, _skillIconContainer);
					Image iconImage = iconObj.GetComponent<Image>();
					if (iconImage != null && info.icon != null)
						iconImage.sprite = info.icon;
					iconObj.transform.localScale = Vector3.zero;
				}
			}
		}

		StartCoroutine(AnimateIcons());
	}

	/// <summary>
	/// 아이콘을 순차적으로 팝 애니메이션 한다.
	/// </summary>
	private IEnumerator AnimateIcons()
	{
		for (int i = 0; i < _skillIconContainer.childCount; i++)
		{
			Transform icon = _skillIconContainer.GetChild(i);
			StartCoroutine(PunchScale(icon));
			yield return new WaitForSecondsRealtime(_iconAnimInterval);
		}
	}

	/// <summary>
	/// 스케일 0 -> 1.2 -> 1.0 팝 애니메이션.
	/// </summary>
	private IEnumerator PunchScale(Transform target)
	{
		float duration = 0.2f;
		float elapsed = 0f;

		// 0 -> 1.2
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			target.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, t);
			yield return null;
		}

		// 1.2 -> 1.0
		elapsed = 0f;
		duration = 0.1f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			target.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t);
			yield return null;
		}
		target.localScale = Vector3.one;
	}

	/// <summary>
	/// 재생 버튼 클릭 시 호출.
	/// </summary>
	public void OnResumeClicked()
	{
		StartCoroutine(HidePanel());
	}

	/// <summary>
	/// 패널 페이드아웃 후 게임 재개.
	/// </summary>
	private IEnumerator HidePanel()
	{
		float elapsed = 0f;
		while (elapsed < _panelFadeDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			_canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _panelFadeDuration);
			yield return null;
		}

		_panel.SetActive(false);
		Time.timeScale = 1f;
		GameManager.Instance.IsPaused = false;
		UIManager.Instance.HidePause();
	}
}
