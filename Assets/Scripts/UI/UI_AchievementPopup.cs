using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 업적 해금 시 토스트 팝업을 표시한다.
/// Queue 기반으로 연속 해금 시 순차 표시하며 TimeScale을 무시한다.
/// </summary>
public class UI_AchievementPopup : MonoBehaviour
{
	[SerializeField] private GameObject _popupRoot;
	[SerializeField] private TextMeshProUGUI _nameText;
	[SerializeField] private TextMeshProUGUI _rewardText;
	[SerializeField] private CanvasGroup _canvasGroup;
	[SerializeField] private float _displayDuration = 2f;
	[SerializeField] private float _fadeDuration = 0.5f;

	private Queue<AchievementTable> _queue = new();
	private bool _isShowing;

	private void Start()
	{
		if (_popupRoot != null)
			_popupRoot.SetActive(false);

		AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
	}

	private void OnDestroy()
	{
		if (AchievementManager.Instance != null)
			AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
	}

	private void OnAchievementUnlocked(AchievementTable achievement)
	{
		_queue.Enqueue(achievement);
		if (!_isShowing)
			StartCoroutine(ShowNextCoroutine());
	}

	private IEnumerator ShowNextCoroutine()
	{
		_isShowing = true;

		while (_queue.Count > 0)
		{
			AchievementTable achievement = _queue.Dequeue();

			if (_nameText != null)
				_nameText.text = achievement.name;
			if (_rewardText != null)
				_rewardText.text = $"+{achievement.rewardAmount} Gold";

			if (_popupRoot != null)
				_popupRoot.SetActive(true);

			// Fade in
			if (_canvasGroup != null)
			{
				_canvasGroup.alpha = 0f;
				float elapsed = 0f;
				while (elapsed < _fadeDuration)
				{
					elapsed += Time.unscaledDeltaTime;
					_canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
					yield return null;
				}
				_canvasGroup.alpha = 1f;
			}

			yield return new WaitForSecondsRealtime(_displayDuration);

			// Fade out
			if (_canvasGroup != null)
			{
				float elapsed = 0f;
				while (elapsed < _fadeDuration)
				{
					elapsed += Time.unscaledDeltaTime;
					_canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
					yield return null;
				}
				_canvasGroup.alpha = 0f;
			}

			if (_popupRoot != null)
				_popupRoot.SetActive(false);

			yield return new WaitForSecondsRealtime(0.2f);
		}

		_isShowing = false;
	}
}
