using UnityEngine;
using TMPro;

/// <summary>
/// "LEVEL UP!" 텍스트가 위로 떠오르며 펀치 스케일 후 페이드아웃.
/// </summary>
public class LevelUpText : MonoBehaviour
{
	[SerializeField] private float _riseSpeed = 1.0f;
	[SerializeField] private float _duration = 1.5f;
	[SerializeField] private float _punchScale = 1.3f;

	private TextMeshPro _tmp;
	private float _elapsed;
	private Vector3 _baseScale;

	private void OnEnable()
	{
		_elapsed = 0f;
	}

	/// <summary>
	/// 텍스트를 초기화한다.
	/// </summary>
	public void Init()
	{
		_tmp = GetComponent<TextMeshPro>();
		_tmp.text = "LEVEL UP!";
		_tmp.color = new Color(1f, 0.85f, 0f, 1f); // gold
		_tmp.fontSize = 8f;
		_tmp.alignment = TextAlignmentOptions.Center;
		_baseScale = Vector3.one;
		transform.localScale = _baseScale * _punchScale;
	}

	private void Update()
	{
		_elapsed += Time.unscaledDeltaTime;
		transform.position += Vector3.up * _riseSpeed * Time.unscaledDeltaTime;

		// Punch scale: 1.3 -> 1.0 in first 0.3s
		float punchT = Mathf.Clamp01(_elapsed / 0.3f);
		float scale = Mathf.Lerp(_punchScale, 1f, punchT);
		transform.localScale = _baseScale * scale;

		// Fade out
		float alpha = Mathf.Clamp01(1f - _elapsed / _duration);
		_tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, alpha);

		// Billboard
		if (Camera.main != null)
			transform.rotation = Camera.main.transform.rotation;

		if (_elapsed >= _duration)
			ObjectPool.Instance.Return(gameObject);
	}
}
