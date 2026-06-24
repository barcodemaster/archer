using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 스킬 선택 후 화면 상단에 스킬명/설명을 슬라이드 애니메이션으로 표시하는 알림 패널.
/// </summary>
public class UI_SkillNotify : MonoBehaviour
{
	[SerializeField] private RectTransform _panel;
	[SerializeField] private TextMeshProUGUI _nameText;
	[SerializeField] private TextMeshProUGUI _descText;

	[Header("Animation")]
	[SerializeField] private float _slideDuration = 0.4f;
	[SerializeField] private float _displayDuration = 1.5f;
	[SerializeField] private float _slideOutDuration = 0.3f;
	[SerializeField] private float _startX = -800f;
	[SerializeField] private float _targetX = 0f;
	[SerializeField] private float _endX = 800f;
	[SerializeField] private float _minScaleX = 0.3f;

	private Coroutine _animCoroutine;

	private void Awake()
	{
		if (_panel != null)
			_panel.gameObject.SetActive(false);
	}

	/// <summary>
	/// 알림을 표시한다. 이미 표시 중이면 기존 애니메이션을 중단하고 재시작.
	/// </summary>
	public void Show(string skillName, string desc)
	{
		gameObject.SetActive(true);

		if (_nameText != null) _nameText.text = skillName;
		if (_descText != null) _descText.text = desc;

		if (_animCoroutine != null)
			StopCoroutine(_animCoroutine);

		_animCoroutine = StartCoroutine(AnimateCoroutine());
	}

	private IEnumerator AnimateCoroutine()
	{
		_panel.gameObject.SetActive(true);

		// 슬라이드 인: 왼쪽 → 중앙, scaleX 0.3 → 1.0 (ease-in quad)
		float t = 0f;
		while (t < _slideDuration)
		{
			t += Time.unscaledDeltaTime;
			float ease = Mathf.Pow(Mathf.Clamp01(t / _slideDuration), 2f);
			float x = Mathf.Lerp(_startX, _targetX, ease);
			float scaleX = Mathf.Lerp(_minScaleX, 1f, ease);
			_panel.anchoredPosition = new Vector2(x, _panel.anchoredPosition.y);
			_panel.localScale = new Vector3(scaleX, 1f, 1f);
			yield return null;
		}
		_panel.anchoredPosition = new Vector2(_targetX, _panel.anchoredPosition.y);
		_panel.localScale = Vector3.one;

		// 대기
		float waited = 0f;
		while (waited < _displayDuration)
		{
			waited += Time.unscaledDeltaTime;
			yield return null;
		}

		// 슬라이드 아웃: 중앙 → 오른쪽, scaleX 1.0 → 0.3 (ease-in quad)
		t = 0f;
		while (t < _slideOutDuration)
		{
			t += Time.unscaledDeltaTime;
			float ease = Mathf.Pow(Mathf.Clamp01(t / _slideOutDuration), 2f);
			float x = Mathf.Lerp(_targetX, _endX, ease);
			float scaleX = Mathf.Lerp(1f, _minScaleX, ease);
			_panel.anchoredPosition = new Vector2(x, _panel.anchoredPosition.y);
			_panel.localScale = new Vector3(scaleX, 1f, 1f);
			yield return null;
		}

		_panel.gameObject.SetActive(false);
		_panel.localScale = Vector3.one;
		_animCoroutine = null;
	}
}
