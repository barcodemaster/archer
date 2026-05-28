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
		PlayBGM();
	}

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
}
