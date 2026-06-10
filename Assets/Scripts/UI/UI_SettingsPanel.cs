using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널. BGM/SFX 볼륨 슬라이더를 제공한다.
/// 프리팹은 Unity 에디터에서 수동 생성 필요.
/// </summary>
public class UI_SettingsPanel : MonoBehaviour
{
	[SerializeField] private Slider _bgmSlider;
	[SerializeField] private Slider _sfxSlider;
	[SerializeField] private Button _closeButton;

	private void Awake()
	{
		if (_bgmSlider != null)
			_bgmSlider.onValueChanged.AddListener(OnBGMChanged);

		if (_sfxSlider != null)
			_sfxSlider.onValueChanged.AddListener(OnSFXChanged);

		if (_closeButton != null)
			_closeButton.onClick.AddListener(Close);
	}

	private void OnEnable()
	{
		if (_bgmSlider != null)
			_bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);

		if (_sfxSlider != null)
			_sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
	}

	private void OnBGMChanged(float value)
	{
		AudioManager.Instance.SetBGMVolume(value);
	}

	private void OnSFXChanged(float value)
	{
		AudioManager.Instance.SetSFXVolume(value);
	}

	public void Open()
	{
		gameObject.SetActive(true);
	}

	public void Close()
	{
		gameObject.SetActive(false);
	}
}
