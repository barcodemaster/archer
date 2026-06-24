using UnityEngine;

/// <summary>
/// 게임 전체 사운드를 관리하는 싱글톤. BGM, SFX, UI SFX 3개의 AudioSource를 사용한다.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
	[Header("Audio Clips")]
	[SerializeField] private AudioClip _footstepClip;
	[SerializeField] private AudioClip _bgmClip;
	[SerializeField] private AudioClip _hitClip;
	[SerializeField] private AudioClip _playerProjectileClip;
	[SerializeField] private AudioClip _monsterProjectileClip;
	[SerializeField] private AudioClip _buttonClickClip;
	[SerializeField] private AudioClip _levelUpClip;
	[SerializeField] private AudioClip _expCollectClip;
	[SerializeField] private AudioClip _upgradeSlotSpinClip;
	[SerializeField] private AudioClip _upgradeSlotConfirmClip;
	[SerializeField] private AudioClip _stageProgressClip;
	[SerializeField] private AudioClip _upgradePanelOpenClip;
	[SerializeField] private AudioClip _landingClip;

	private AudioSource _bgmSource;
	private AudioSource _sfxSource;
	private AudioSource _uiSfxSource;

	private void Awake()
	{
		_bgmSource = gameObject.AddComponent<AudioSource>();
		_bgmSource.loop = true;
		_bgmSource.playOnAwake = false;

		_sfxSource = gameObject.AddComponent<AudioSource>();
		_sfxSource.playOnAwake = false;

		_uiSfxSource = gameObject.AddComponent<AudioSource>();
		_uiSfxSource.playOnAwake = false;
		_uiSfxSource.ignoreListenerPause = true;
	}

	private void Start()
	{
		_bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);
		_sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
		_uiSfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
		PlayBGM();
	}

	/// <summary>
	/// BGM 볼륨을 설정하고 PlayerPrefs에 저장한다.
	/// </summary>
	public void SetBGMVolume(float volume)
	{
		volume = Mathf.Clamp01(volume);
		_bgmSource.volume = volume;
		PlayerPrefs.SetFloat("BGMVolume", volume);
	}

	/// <summary>
	/// SFX 볼륨을 설정하고 PlayerPrefs에 저장한다.
	/// </summary>
	public void SetSFXVolume(float volume)
	{
		volume = Mathf.Clamp01(volume);
		_sfxSource.volume = volume;
		_uiSfxSource.volume = volume;
		PlayerPrefs.SetFloat("SFXVolume", volume);
	}

	public float BGMVolume => _bgmSource != null ? _bgmSource.volume : 1f;
	public float SFXVolume => _sfxSource != null ? _sfxSource.volume : 1f;

	public void PlayBGM()
	{
		if (_bgmClip == null) return;
		_bgmSource.clip = _bgmClip;
		_bgmSource.Play();
	}

	public void PlayFootstep()
	{
		if (_footstepClip != null)
			_sfxSource.PlayOneShot(_footstepClip);
	}

	public void PlayHit()
	{
		if (_hitClip != null)
			_sfxSource.PlayOneShot(_hitClip);
	}

	public void PlayPlayerProjectile()
	{
		if (_playerProjectileClip != null)
			_sfxSource.PlayOneShot(_playerProjectileClip);
	}

	public void PlayMonsterProjectile()
	{
		if (_monsterProjectileClip != null)
			_sfxSource.PlayOneShot(_monsterProjectileClip);
	}

	public void PlayButtonClick()
	{
		if (_buttonClickClip != null)
			_uiSfxSource.PlayOneShot(_buttonClickClip);
	}

	public void PlayLevelUp()
	{
		if (_levelUpClip != null)
			_uiSfxSource.PlayOneShot(_levelUpClip);
	}

	public void PlayExpCollect()
	{
		if (_expCollectClip != null)
			_sfxSource.PlayOneShot(_expCollectClip);
	}

	public void PlayUpgradeSlotSpin()
	{
		if (_upgradeSlotSpinClip != null)
			_uiSfxSource.PlayOneShot(_upgradeSlotSpinClip);
	}

	public void PlayUpgradeSlotConfirm()
	{
		if (_upgradeSlotConfirmClip != null)
			_uiSfxSource.PlayOneShot(_upgradeSlotConfirmClip);
	}

	public void PlayStageProgress()
	{
		if (_stageProgressClip != null)
			_sfxSource.PlayOneShot(_stageProgressClip);
	}

	public void PlayUpgradePanelOpen()
	{
		if (_upgradePanelOpenClip != null)
			_uiSfxSource.PlayOneShot(_upgradePanelOpenClip);
	}

	public void PlayLanding()
	{
		if (_landingClip != null)
			_sfxSource.PlayOneShot(_landingClip);
	}

	/// <summary>
	/// 임의의 AudioClip을 SFX 채널로 재생한다.
	/// </summary>
	public void PlaySfx(AudioClip clip)
	{
		if (clip != null)
			_sfxSource.PlayOneShot(clip);
	}

	/// <summary>
	/// BGM을 페이드 아웃한다.
	/// </summary>
	public void FadeBGMOut(float duration = -1f)
	{
		if (duration < 0f) duration = GameConfig.Instance.bgmFadeDuration;
		StartCoroutine(FadeBGMCoroutine(_bgmSource.volume, 0f, duration));
	}

	/// <summary>
	/// BGM을 페이드 인한다.
	/// </summary>
	public void FadeBGMIn(float duration = -1f)
	{
		if (duration < 0f) duration = GameConfig.Instance.bgmFadeDuration;
		float targetVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
		StartCoroutine(FadeBGMCoroutine(0f, targetVolume, duration));
	}

	/// <summary>
	/// 새로운 BGM으로 크로스페이드한다.
	/// </summary>
	public void CrossFadeBGM(AudioClip newClip, float duration = -1f)
	{
		if (duration < 0f) duration = GameConfig.Instance.bgmFadeDuration;
		StartCoroutine(CrossFadeBGMCoroutine(newClip, duration));
	}

	private System.Collections.IEnumerator FadeBGMCoroutine(float from, float to, float duration)
	{
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			_bgmSource.volume = Mathf.Lerp(from, to, elapsed / duration);
			yield return null;
		}
		_bgmSource.volume = to;
		if (to <= 0f) _bgmSource.Stop();
	}

	private System.Collections.IEnumerator CrossFadeBGMCoroutine(AudioClip newClip, float duration)
	{
		float halfDuration = duration * 0.5f;
		yield return FadeBGMCoroutine(_bgmSource.volume, 0f, halfDuration);
		_bgmSource.clip = newClip;
		_bgmSource.Play();
		float targetVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
		yield return FadeBGMCoroutine(0f, targetVolume, halfDuration);
	}

	/// <summary>
	/// 앱이 포커스를 잃으면(전화 수신 등) 오디오를 일시정지하고, 복귀 시 재개한다.
	/// </summary>
	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			AudioListener.pause = true;
		}
		else
		{
			AudioListener.pause = false;
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		AudioListener.pause = !hasFocus;
	}
}
