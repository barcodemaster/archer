using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 숫자가 위로 떠오르며 서서히 사라지는 월드 스페이스 텍스트.
/// </summary>
public class DamageText : MonoBehaviour
{
	[SerializeField] private float _riseSpeed = 1.5f;
	[SerializeField] private float _duration = 1f;

	private TextMeshPro _tmp;
	private float _elapsed;
	private Color _color;

	private void OnEnable()
	{
		_elapsed = 0f;
	}

	/// <summary>
	/// 데미지 값과 피격 대상 종류에 따라 텍스트를 초기화한다.
	/// </summary>
	public void Init(float damage, bool isPlayer)
	{
		_tmp = GetComponent<TextMeshPro>();
		_tmp.text = Mathf.RoundToInt(damage).ToString();
		_color = isPlayer ? Color.white : Color.white;
		_tmp.color = _color;
	}

	private void Update()
	{
		_elapsed += Time.deltaTime;
		transform.position += Vector3.up * _riseSpeed * Time.deltaTime;

		float alpha = Mathf.Clamp01(1f - _elapsed / _duration);
		_tmp.color = new Color(_color.r, _color.g, _color.b, alpha);

		if (Camera.main != null)
			transform.rotation = Camera.main.transform.rotation;

		if (_elapsed >= _duration)
			ObjectPool.Instance.Return(gameObject);
	}
}
