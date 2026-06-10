using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
	[SerializeField] protected Slider _slider;
	[SerializeField] Slider _trailSlider;

	[SerializeField] float _drainRate = 0.1f;      // 초당 줄어드는 비율 (0.5 = 초당 50%)
	[SerializeField] float _drainDelay = 0.5f;      // 첫 히트 후 줄어들기 시작까지 딜레이
	float _drainDelayTimer;

	/// <summary>
	/// HP 슬라이더 값을 갱신한다. 트레일 슬라이더는 딜레이 후 일정 속도로 따라간다.
	/// </summary>
	public virtual void SetHP(float current, float max)
	{
		if (_slider == null || max <= 0)
			return;

		float ratio = current / max;
		_slider.value = ratio;

		if (_trailSlider == null)
			return;

		if (ratio >= _trailSlider.value)
		{
			// HP 증가 또는 동일 -> 트레일도 즉시 갱신
			_trailSlider.value = ratio;
			_drainDelayTimer = 0f;
		}
		else
		{
			// 아직 감소 시작 전이면 딜레이 시작 (이미 감소 중이면 리셋하지 않음)
			if (_drainDelayTimer <= 0f && _trailSlider.value <= _slider.value + 0.001f)
				_drainDelayTimer = _drainDelay;
		}
	}

	protected virtual void LateUpdate()
	{
		// 빌보드: 탑뷰 카메라를 향하도록 회전
		Camera cam = Camera.main;
		if (cam != null)
		{
			transform.rotation = cam.transform.rotation;
		}

		if (_trailSlider == null)
			return;

		// 트레일 드레인 처리
		if (_trailSlider.value > _slider.value)
		{
			if (_drainDelayTimer > 0f)
			{
				_drainDelayTimer -= Time.deltaTime;
			}
			else
			{
				_trailSlider.value -= _drainRate * Time.deltaTime;
				if (_trailSlider.value < _slider.value)
					_trailSlider.value = _slider.value;
			}
		}
	}
}
