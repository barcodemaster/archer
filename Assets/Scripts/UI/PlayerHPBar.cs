using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBar : HPBar
{
	[SerializeField] private Text _hpText;
	[SerializeField] private RectTransform _dividerContainer;
	[SerializeField] private GameObject _dividerPrefab;

	private float _lastMaxHp;
	private List<GameObject> _dividers = new List<GameObject>();

	private void Awake()
	{
		if (_dividerContainer != null)
		{
			_dividerContainer.SetParent(transform, true);
		}
	}

	/// <summary>
	/// HP 슬라이더, 텍스트, 구분선을 갱신한다.
	/// </summary>
	public override void SetHP(float current, float max)
	{
		base.SetHP(current, max);

		if (_hpText != null)
			_hpText.text = $"{(int)current}";

		if (max != _lastMaxHp)
		{
			_lastMaxHp = max;
			RebuildDividers(max);
		}
	}

	/// <summary>
	/// 200HP 단위 구분선을 재생성한다.
	/// </summary>
	private void RebuildDividers(float maxHp)
	{
		foreach (GameObject d in _dividers)
			Destroy(d);
		_dividers.Clear();

		if (_dividerContainer == null || _dividerPrefab == null)
			return;

		int dividerCount = (int)(maxHp / 200f);
		float containerWidth = _dividerContainer.rect.width;

		for (int i = 1; i < dividerCount + 1; i++)
		{
			float ratio = (i * 200f) / maxHp;
			if (ratio >= 1f)
				break;

			GameObject divider = Instantiate(_dividerPrefab, _dividerContainer);
			RectTransform rt = divider.GetComponent<RectTransform>();
			if (rt != null)
			{
				rt.anchorMin = new Vector2(ratio, 0);
				rt.anchorMax = new Vector2(ratio, 1);
				rt.sizeDelta = new Vector2(2f, 0);
				rt.anchoredPosition = Vector2.zero;
			}
			_dividers.Add(divider);
		}
	}
}
