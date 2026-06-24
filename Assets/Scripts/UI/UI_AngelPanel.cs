using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

/// <summary>
/// 천사방 축복 선택 패널. 왼쪽: 랜덤 능력, 오른쪽: 체력 회복.
/// </summary>
public class UI_AngelPanel : MonoBehaviour
{
	[Header("Title")]
	[SerializeField] private TextMeshProUGUI _titleText;
	[SerializeField] private TextMeshProUGUI _subtitleText;

	[Header("Angel Image")]
	[SerializeField] private Image _angelImage;
	[SerializeField] private Sprite _angelSprite;

	[Header("Left - Upgrade")]
	[SerializeField] private Button _upgradeButton;
	[SerializeField] private Image _upgradeIcon;
	[SerializeField] private TextMeshProUGUI _upgradeName;

	[Header("Right - Heal")]
	[SerializeField] private Button _healButton;
	[SerializeField] private Image _healIcon;
	[SerializeField] private TextMeshProUGUI _healName;

	[Header("Animation")]
	[SerializeField] private RectTransform _contentRoot;

	[Header("Notification")]
	[SerializeField] private UI_SkillNotify _skillNotify;

	private static readonly EUpgradeType[] _angelUpgradePool = new[]
	{
		EUpgradeType.AttackBoost,
		EUpgradeType.AttackSpeed,
		EUpgradeType.CriticalMaster,
		EUpgradeType.HpBoost,
	};

	private UpgradeInfo _selectedUpgrade;
	private AngelNPC _angelNPC;

	private Coroutine _animCoroutine;

	private void Awake()
	{
		_upgradeButton.onClick.AddListener(OnUpgradeSelected);
		_healButton.onClick.AddListener(OnHealSelected);
	}

	/// <summary>
	/// 패널을 열고 랜덤 능력을 배치한다.
	/// </summary>
	public void Show(AngelNPC angel = null)
	{
		_angelNPC = angel;
		gameObject.SetActive(true);

		// 랜덤 능력 선택
		EUpgradeType type = _angelUpgradePool[Random.Range(0, _angelUpgradePool.Length)];
		_selectedUpgrade = UpgradeDatabase.GetInfo(type);

		if (_selectedUpgrade != null)
		{
			if (_upgradeIcon != null) _upgradeIcon.sprite = _selectedUpgrade.icon;
			if (_upgradeName != null) _upgradeName.text = _selectedUpgrade.name;
		}

		// Heal 아이콘/이름
		if (_healIcon != null)
			_healIcon.sprite = IconHelper.GetSprite("Heal");
		if (_healName != null)
			_healName.text = "체력 회복";

		// Title / SubTitle
		if (_titleText != null)
			_titleText.text = "천사를 발견했어요!";
		if (_subtitleText != null)
			_subtitleText.text = "축복을 선택하세요!";

		// 천사 이미지 셋업
		if (_angelImage != null && _angelSprite != null)
			_angelImage.sprite = _angelSprite;

		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;
		UIManager.Instance.ResetEventSystem();

		// 등장 애니메이션
		if (_animCoroutine != null)
			StopCoroutine(_animCoroutine);
		_animCoroutine = StartCoroutine(ShowAnimation());
	}

	/// <summary>
	/// 등장 애니메이션: Title 슬라이드 → 나머지 팝.
	/// </summary>
	private IEnumerator ShowAnimation()
	{
		// 버튼 비활성화
		_upgradeButton.interactable = false;
		_healButton.interactable = false;

		// SubTitle + ContentRoot 초기 숨김
		if (_subtitleText != null)
			_subtitleText.transform.localScale = Vector3.zero;
		if (_contentRoot != null)
			_contentRoot.localScale = Vector3.zero;

		// 1) Title 슬라이드: 왼쪽 밖 → 원래 위치
		if (_titleText != null)
		{
			RectTransform titleRT = _titleText.GetComponent<RectTransform>();
			Vector2 originalPos = titleRT.anchoredPosition;
			Vector2 startPos = originalPos + Vector2.left * 800f;
			titleRT.anchoredPosition = startPos;

			float elapsed = 0f;
			float duration = 0.4f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float eased = 1f - (1f - t) * (1f - t); // EaseOut quadratic
				titleRT.anchoredPosition = Vector2.Lerp(startPos, originalPos, eased);
				yield return null;
			}
			titleRT.anchoredPosition = originalPos;
		}

		// 2) SubTitle + ContentRoot 팝: scale 0 → 1.15 → 1.0
		float popElapsed = 0f;
		float popDuration = 0.3f;
		while (popElapsed < popDuration)
		{
			popElapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(popElapsed / popDuration);
			float scale;
			if (t < 0.7f)
			{
				// 0 → 1.15 (70% of duration)
				float st = t / 0.7f;
				scale = Mathf.Lerp(0f, 1.15f, st);
			}
			else
			{
				// 1.15 → 1.0 (30% of duration)
				float st = (t - 0.7f) / 0.3f;
				scale = Mathf.Lerp(1.15f, 1f, st);
			}

			Vector3 s = Vector3.one * scale;
			if (_subtitleText != null)
				_subtitleText.transform.localScale = s;
			if (_contentRoot != null)
				_contentRoot.localScale = s;
			yield return null;
		}

		if (_subtitleText != null)
			_subtitleText.transform.localScale = Vector3.one;
		if (_contentRoot != null)
			_contentRoot.localScale = Vector3.one;

		// 버튼 활성화
		_upgradeButton.interactable = true;
		_healButton.interactable = true;

		_animCoroutine = null;
	}

	/// <summary>
	/// 왼쪽 버튼: 능력 업그레이드 적용.
	/// </summary>
	private void OnUpgradeSelected()
	{
		if (_selectedUpgrade == null) return;

		PlayerController player = PlayerController.Instance;
		if (player != null)
		{
			PlayerUpgrade upgrade = player.GetComponent<PlayerUpgrade>();
			if (upgrade != null)
				upgrade.AddUpgrade(_selectedUpgrade.type);
			player.ApplyUpgradeEffect(_selectedUpgrade.type);
		}

		if (_skillNotify != null && _selectedUpgrade != null)
			_skillNotify.Show(_selectedUpgrade.name, _selectedUpgrade.description);
		StartCoroutine(PunchScale(_upgradeButton.transform));

		Close();
	}

	/// <summary>
	/// 오른쪽 버튼: 최대 체력의 30% 회복.
	/// </summary>
	private void OnHealSelected()
	{
		PlayerController player = PlayerController.Instance;
		if (player != null)
			player.Heal(player.MaxHp * 0.3f);

		if (_skillNotify != null)
			_skillNotify.Show("체력 회복", "최대 체력의 30%를 회복합니다");
		StartCoroutine(PunchScale(_healButton.transform));

		Close();
	}

	private IEnumerator PunchScale(Transform target)
	{
		target.localScale = Vector3.one * 1.15f;
		float t = 0f;
		while (t < 0.2f)
		{
			t += Time.unscaledDeltaTime;
			target.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t / 0.2f);
			yield return null;
		}
		target.localScale = Vector3.one;
	}

	/// <summary>
	/// 패널을 닫고 ExitDoor를 연다.
	/// </summary>
	private void Close()
	{
		if (_animCoroutine != null)
		{
			StopCoroutine(_animCoroutine);
			_animCoroutine = null;
		}

		// 3D 천사 상승 연출
		_angelNPC?.PlayAscendAnimation();

		UIManager.Instance.HideAngelPanel();
		StageManager.Instance.OnAngelBlessingSelected();
	}

}
