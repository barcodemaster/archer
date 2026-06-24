using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

/// <summary>
/// 일시정지 버튼 + 습득 스킬 표시 패널.
/// </summary>
public class UI_PausePanel : MonoBehaviour
{
	[SerializeField] private GameObject _panel;
	[SerializeField] private Button _pauseButton;
	[SerializeField] private Button _resumeButton;
	[SerializeField] private Transform _skillIconContainer;
	[SerializeField] private GameObject _skillIconPrefab;
	[SerializeField] private SkillIconPool _skillIconPool;
	[SerializeField] private float _iconAnimInterval = 0.1f;
	[SerializeField] private float _panelFadeDuration = 0.3f;
	[SerializeField] private TMP_FontAsset _koreanFont;
	[SerializeField] private float _tooltipNameFontSize = 28f;
	[SerializeField] private float _tooltipDescFontSize = 22f;

	private CanvasGroup _canvasGroup;

	private GameObject _tooltipObj;
	private TextMeshProUGUI _tooltipNameText;
	private TextMeshProUGUI _tooltipDescText;
	private RectTransform _tooltipRect;
	private RectTransform _lastClickedIcon;

	private void Start()
	{
		if (_panel != null)
		{
			_panel.SetActive(false);
			_canvasGroup = _panel.GetComponent<CanvasGroup>();
			if (_canvasGroup == null)
				_canvasGroup = _panel.AddComponent<CanvasGroup>();
		}

		if (_pauseButton != null)
			_pauseButton.onClick.AddListener(OnPauseClicked);
		if (_resumeButton != null)
			_resumeButton.onClick.AddListener(OnResumeClicked);

		// 자동으로 SkillIconPool을 찾아 할당 (Inspector에 할당 없을 경우)
		if (_skillIconPool == null)
		{
			_skillIconPool = Object.FindAnyObjectByType<SkillIconPool>();
			if (_skillIconPool == null)
			{
				var poolObj = new GameObject("SkillIconPool");
				_skillIconPool = poolObj.AddComponent<SkillIconPool>();
			}
			// Prefab 초기화
			if (_skillIconPool != null && _skillIconPrefab != null)
				_skillIconPool.SetPrefab(_skillIconPrefab);
		}

		CreateTooltip();
		CreateCheatButton();
	}

	/// <summary>
	/// 일시정지 버튼 클릭 시 호출.
	/// </summary>
	public void OnPauseClicked()
	{
		if (GameManager.Instance.IsPaused) return;

		HideTooltip();

		Time.timeScale = 0f;
		GameManager.Instance.IsPaused = true;

		_panel.SetActive(true);
		_canvasGroup.alpha = 1f;
		UIManager.Instance.ShowPause();

		// 기존 아이콘 제거
		for (int i = _skillIconContainer.childCount - 1; i >= 0; i--)
		{
			var child = _skillIconContainer.GetChild(i).gameObject;
			_skillIconPool.Release(child);
		}

		// 습득한 스킬 표시
		PlayerUpgrade playerUpgrade = PlayerController.Instance?.GetComponent<PlayerUpgrade>();
		if (playerUpgrade != null)
		{
			UpgradeInfo[] allUpgrades = UpgradeDatabase.GetAll();
			foreach (var info in allUpgrades)
			{
				if (playerUpgrade.GetLevel(info.type) > 0)
				{
					// 풀에서 아이콘 오브젝트 가져오기
					GameObject iconObj = _skillIconPool.Get(_skillIconContainer);
					var img = iconObj.GetComponent<Image>();
					if (img != null && info.icon != null)
					{
						img.material = null;
						img.sprite = info.icon;
						img.canvasRenderer.SetAlpha(1f);

					}

					// 초기 스케일 0 로 설정 (애니메이션을 위해)
					iconObj.transform.localScale = Vector3.zero;

					// 버튼 설정 (프리팹에 이미 있으면 재사용)
					Button btn = iconObj.GetComponent<Button>();
					if (btn == null) btn = iconObj.AddComponent<Button>();
					RectTransform iconRect = iconObj.GetComponent<RectTransform>();
					btn.onClick.RemoveAllListeners();
					btn.onClick.AddListener(() => ShowTooltip(iconRect, info));
				}
			}
		}

		StartCoroutine(AnimateIcons());
	}

	/// <summary>
	/// 아이콘을 순차적으로 팝 애니메이션 한다.
	/// </summary>
	private IEnumerator AnimateIcons()
	{
		for (int i = 0; i < _skillIconContainer.childCount; i++)
		{
			Transform icon = _skillIconContainer.GetChild(i);
			StartCoroutine(PunchScale(icon));
			yield return new WaitForSecondsRealtime(_iconAnimInterval);
		}
	}

	/// <summary>
	/// 스케일 0 -> 1.2 -> 1.0 팝 애니메이션.
	/// </summary>
	private IEnumerator PunchScale(Transform target)
	{
		float duration = 0.2f;
		float elapsed = 0f;

		// 0 -> 1.2
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			target.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, t);
			yield return null;
		}

		// 1.2 -> 1.0
		elapsed = 0f;
		duration = 0.1f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			target.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t);
			yield return null;
		}
		target.localScale = Vector3.one;
	}

	/// <summary>
	/// 재생 버튼 클릭 시 호출.
	/// </summary>
	public void OnResumeClicked()
	{
		HideTooltip();
		StartCoroutine(HidePanel());
	}

	/// <summary>
	/// 패널 페이드아웃 후 게임 재개.
	/// </summary>
	private IEnumerator HidePanel()
	{
		float elapsed = 0f;
		while (elapsed < _panelFadeDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			_canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _panelFadeDuration);
			yield return null;
		}

		_panel.SetActive(false);
		Time.timeScale = 1f;
		GameManager.Instance.IsPaused = false;
		UIManager.Instance.HidePause();
	}

	/// <summary>
	/// 툴팁 UI를 _panel 하위에 동적 생성한다.
	/// </summary>
	private void CreateTooltip()
	{
		if (_panel == null) return;

		_tooltipObj = new GameObject("SkillTooltip", typeof(RectTransform), typeof(Image),
			typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
		_tooltipObj.transform.SetParent(_panel.transform, false);
		_tooltipRect = _tooltipObj.GetComponent<RectTransform>();

		// 반투명 검정 배경
		Image bg = _tooltipObj.GetComponent<Image>();
		bg.color = new Color(0f, 0f, 0f, 0.85f);

		// 레이아웃 설정
		VerticalLayoutGroup vlg = _tooltipObj.GetComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(12, 12, 8, 8);
		vlg.spacing = 4f;
		vlg.childAlignment = TextAnchor.UpperLeft;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		ContentSizeFitter csf = _tooltipObj.GetComponent<ContentSizeFitter>();
		csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		// preferredWidth 제한용 LayoutElement
		LayoutElement le = _tooltipObj.AddComponent<LayoutElement>();
		le.preferredWidth = 300f;

		// 이름 텍스트
		GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
		nameObj.transform.SetParent(_tooltipObj.transform, false);
		_tooltipNameText = nameObj.GetComponent<TextMeshProUGUI>();
		_tooltipNameText.fontSize = _tooltipNameFontSize;
		_tooltipNameText.fontStyle = FontStyles.Bold;
		_tooltipNameText.color = Color.white;
		_tooltipNameText.enableWordWrapping = true;

		// 설명 텍스트
		GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
		descObj.transform.SetParent(_tooltipObj.transform, false);
		_tooltipDescText = descObj.GetComponent<TextMeshProUGUI>();
		_tooltipDescText.fontSize = _tooltipDescFontSize;
		_tooltipDescText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
		_tooltipDescText.enableWordWrapping = true;

		if (_koreanFont != null)
		{
			_tooltipNameText.font = _koreanFont;
			_tooltipDescText.font = _koreanFont;
		}

		_tooltipObj.SetActive(false);
	}

	/// <summary>
	/// 아이콘 클릭 시 툴팁을 표시하거나 토글한다.
	/// </summary>
	private void ShowTooltip(RectTransform iconRect, UpgradeInfo info)
	{
		// 같은 아이콘 재클릭 시 토글
		if (_lastClickedIcon == iconRect && _tooltipObj.activeSelf)
		{
			HideTooltip();
			return;
		}

		_lastClickedIcon = iconRect;
		_tooltipNameText.text = info.name;
		_tooltipDescText.text = info.description;

		// 컨테이너 내 아이콘 상대 위치 비율로 좌우 정렬 결정
		RectTransform containerRect = _skillIconContainer as RectTransform;
		if (containerRect == null)
			containerRect = _skillIconContainer.GetComponent<RectTransform>();

		Vector3 iconLocalPos = containerRect.InverseTransformPoint(iconRect.position);
		float containerWidth = containerRect.rect.width;
		float normalizedX = (iconLocalPos.x - containerRect.rect.xMin) / containerWidth;

		if (normalizedX < 0.5f)
			_tooltipRect.pivot = new Vector2(0f, 1f); // 왼쪽 정렬, 오른쪽으로 펼침
		else
			_tooltipRect.pivot = new Vector2(1f, 1f); // 오른쪽 정렬, 왼쪽으로 펼침

		// 아이콘 아래쪽에 배치
		Vector3 iconPosInPanel = _panel.transform.InverseTransformPoint(iconRect.position);
		float iconHalfH = iconRect.rect.height * 0.5f;
		float gap = 6f;
		_tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
		_tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
		_tooltipRect.anchoredPosition = new Vector2(iconPosInPanel.x, iconPosInPanel.y - iconHalfH - gap);

		_tooltipObj.SetActive(true);
		_tooltipObj.transform.SetAsLastSibling();
	}

	/// <summary>
	/// 툴팁을 숨긴다.
	/// </summary>
	private void HideTooltip()
	{
		if (_tooltipObj != null) _tooltipObj.SetActive(false);
		_lastClickedIcon = null;
	}

	/// <summary>
	/// 패널 하단에 빨간색 CHEAT 버튼을 동적 생성한다.
	/// </summary>
	private void CreateCheatButton()
	{
		if (_panel == null) return;

		GameObject btnObj = new GameObject("CheatButton", typeof(RectTransform), typeof(Image), typeof(Button));
		btnObj.transform.SetParent(_panel.transform, false);

		RectTransform btnRT = btnObj.GetComponent<RectTransform>();
		btnRT.anchorMin = new Vector2(0.5f, 0f);
		btnRT.anchorMax = new Vector2(0.5f, 0f);
		btnRT.pivot = new Vector2(0.5f, 0f);
		btnRT.anchoredPosition = new Vector2(0f, 20f);
		btnRT.sizeDelta = new Vector2(200f, 50f);

		Image btnBg = btnObj.GetComponent<Image>();
		btnBg.color = new Color(0.8f, 0.15f, 0.15f, 1f);

		Button btn = btnObj.GetComponent<Button>();
		btn.targetGraphic = btnBg;
		btn.onClick.AddListener(OnCheatClicked);

		GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
		labelObj.transform.SetParent(btnObj.transform, false);
		RectTransform labelRT = labelObj.GetComponent<RectTransform>();
		labelRT.anchorMin = Vector2.zero;
		labelRT.anchorMax = Vector2.one;
		labelRT.offsetMin = Vector2.zero;
		labelRT.offsetMax = Vector2.zero;

		TextMeshProUGUI labelTMP = labelObj.GetComponent<TextMeshProUGUI>();
		labelTMP.text = "CHEAT";
		labelTMP.fontSize = 26f;
		labelTMP.color = Color.white;
		labelTMP.fontStyle = FontStyles.Bold;
		labelTMP.alignment = TextAlignmentOptions.Center;
		if (_koreanFont != null) labelTMP.font = _koreanFont;
	}

	/// <summary>
	/// CHEAT 버튼 클릭 시 치트 패널을 연다.
	/// </summary>
	private void OnCheatClicked()
	{
		UIManager.Instance.ShowCheat();
	}
}
