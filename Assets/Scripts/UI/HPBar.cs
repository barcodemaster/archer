using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
	[SerializeField] protected Slider _slider;

	/// <summary>
	/// HP 슬라이더 값을 갱신한다.
	/// </summary>
	public virtual void SetHP(float current, float max)
	{
		if (_slider != null && max > 0)
			_slider.value = current / max;
	}

	protected virtual void LateUpdate()
	{
		// 빌보드: 탑뷰 카메라를 향하도록 회전
		Camera cam = Camera.main;
		if (cam != null)
		{
			transform.rotation = cam.transform.rotation;
		}
	}
}
