using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 일시정지 메뉴에서 접근하는 업적 목록 패널.
/// ScrollRect + Content로 업적을 나열하며 해금 상태와 진행바를 표시한다.
/// </summary>
public class UI_AchievementPanel : MonoBehaviour
{
	[SerializeField] private Transform _content;
	[SerializeField] private GameObject _slotPrefab;
	[SerializeField] private GameObject _panel;
	[SerializeField] private Button _closeButton;

	private void Start()
	{
		if (_closeButton != null)
			_closeButton.onClick.AddListener(Close);
		if (_panel != null)
			_panel.SetActive(false);
	}

	/// <summary>
	/// 패널을 열고 업적 목록을 갱신한다.
	/// </summary>
	public void Open()
	{
		if (_panel != null)
			_panel.SetActive(true);
		Refresh();
	}

	/// <summary>
	/// 패널을 닫는다.
	/// </summary>
	public void Close()
	{
		if (_panel != null)
			_panel.SetActive(false);
	}

	/// <summary>
	/// 업적 목록을 최신 진행도로 갱신한다.
	/// </summary>
	private void Refresh()
	{
		if (_content == null || _slotPrefab == null) return;

		// 기존 슬롯 제거
		for (int i = _content.childCount - 1; i >= 0; i--)
			Destroy(_content.GetChild(i).gameObject);

		AchievementTable[] all = AchievementDatabase.GetAll();
		AchievementManager manager = AchievementManager.Instance;

		for (int i = 0; i < all.Length; i++)
		{
			AchievementTable achievement = all[i];
			GameObject slot = Instantiate(_slotPrefab, _content);

			bool unlocked = manager.IsUnlocked(achievement.id);
			int progress = manager.GetProgress(achievement.type);
			float ratio = Mathf.Clamp01((float)progress / achievement.target);

			// 이름
			TextMeshProUGUI nameText = slot.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
			if (nameText != null)
				nameText.text = achievement.name;

			// 설명
			TextMeshProUGUI descText = slot.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
			if (descText != null)
				descText.text = achievement.description;

			// 진행도 텍스트
			TextMeshProUGUI progressText = slot.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
			if (progressText != null)
				progressText.text = unlocked ? "Complete!" : $"{progress} / {achievement.target}";

			// 진행바
			Slider progressBar = slot.GetComponentInChildren<Slider>();
			if (progressBar != null)
				progressBar.value = ratio;

			// 보상 텍스트
			TextMeshProUGUI rewardText = slot.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
			if (rewardText != null)
				rewardText.text = $"+{achievement.rewardAmount} Gold";

			// 해금 상태 시각 표시
			Image bg = slot.GetComponent<Image>();
			if (bg != null)
				bg.color = unlocked ? new Color(0.8f, 1f, 0.8f) : Color.white;
		}
	}
}
