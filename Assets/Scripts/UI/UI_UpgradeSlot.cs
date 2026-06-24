using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UI_UpgradeSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] private TextMeshProUGUI _nameText;
	[SerializeField] private Image _iconImage;
	[SerializeField] private TextMeshProUGUI _descText;
	[SerializeField] private Button _button;

	private UpgradeInfo _assigned;
	private Coroutine _spinCoroutine;
	private Coroutine _snapCoroutine;
	private Coroutine _shrinkCoroutine;

	private int _slotIndex;
	private bool _isSelectable;
	public System.Action<int> OnSlotConfirmed;

	// 스트립 스크롤 관련
	private RectTransform _strip;
	private List<Image> _stripIcons = new List<Image>();
	private List<UpgradeInfo> _stripInfos = new List<UpgradeInfo>();
	private RectTransform _viewport;
	private GameObject _iconViewportGo;
	private Vector2 _origIconAnchoredPos;
	private Transform _origIconParent;
	private float _iconHeight;
	private const int STRIP_COUNT = 5;
	private bool _stopping;
	private bool _finalIconPlaced;

	public UpgradeInfo Assigned => _assigned;

	/// <summary>
	/// 슬롯 인덱스와 선택 콜백을 초기화한다.
	/// </summary>
	public void Init(int index, System.Action<int> callback)
	{
		_slotIndex = index;
		OnSlotConfirmed = callback;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!_isSelectable) return;
		_shrinkCoroutine = StartCoroutine(ShrinkCoroutine());
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!_isSelectable) return;
		if (_shrinkCoroutine != null)
		{
			StopCoroutine(_shrinkCoroutine);
			_shrinkCoroutine = null;
		}
		transform.localScale = Vector3.one;
		OnSlotConfirmed?.Invoke(_slotIndex);
	}

	private IEnumerator ShrinkCoroutine()
	{
		float t = 0f;
		float duration = 0.15f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			float ease = Mathf.Pow(Mathf.Clamp01(t / duration), 2f);
			float scale = Mathf.Lerp(1f, 0.8f, ease);
			transform.localScale = Vector3.one * scale;
			yield return null;
		}
		transform.localScale = Vector3.one * 0.8f;
	}

	/// <summary>
	/// 슬롯의 표시 내용을 초기화한다.
	/// </summary>
	public void Clear()
	{
		if (_nameText != null) _nameText.text = "";
		if (_iconImage != null) _iconImage.sprite = null;
		if (_descText != null) _descText.text = "";
	}

	public void SetUpgrade(UpgradeInfo info)
	{
		_assigned = info;
	}

	public void StartSpin()
	{
		// 이전 스핀이 아직 실행 중이면 정리
		if (_spinCoroutine != null)
		{
			StopCoroutine(_spinCoroutine);
			_spinCoroutine = null;
		}
		if (_snapCoroutine != null)
		{
			StopCoroutine(_snapCoroutine);
			_snapCoroutine = null;
		}
		CleanupStrip();

		SetInteractable(false);
		_stopping = false;
		_finalIconPlaced = false;
		SetupStrip();
		_spinCoroutine = StartCoroutine(ScrollCoroutine());
	}

	/// <summary>
	/// 스핀 정지. immediate=true이면 즉시 정지, false이면 감속 후 정지.
	/// </summary>
	public void StopSpin(bool playConfirmSound = true)
	{
		if (!playConfirmSound)
		{
			// 즉시 정지 (스킵 모드)
			if (_spinCoroutine != null)
			{
				StopCoroutine(_spinCoroutine);
				_spinCoroutine = null;
			}
			CleanupStrip();
			Display(_assigned);
			StartCoroutine(PunchScale());
			return;
		}

		// 감속 정지
		_stopping = true;
	}

	public void SetInteractable(bool v)
	{
		_button.enabled = v;
		_isSelectable = v;
	}

	/// <summary>
	/// 스크롤 스트립을 초기화한다. 코드에서 IconViewport를 생성하여 클리핑.
	/// </summary>
	private void SetupStrip()
	{
		RectTransform iconRT = _iconImage.rectTransform;
		_iconHeight = iconRT.rect.height;
		float iconWidth = iconRT.rect.width;

		// 원본 아이콘 위치/부모 저장
		_origIconParent = iconRT.parent;
		_origIconAnchoredPos = iconRT.anchoredPosition;

		// IconViewport 생성 (아이콘과 동일 크기, 동일 위치)
		_iconViewportGo = new GameObject("IconViewport", typeof(RectTransform), typeof(RectMask2D));
		RectTransform vpRT = _iconViewportGo.GetComponent<RectTransform>();
		vpRT.SetParent(_origIconParent, false);
		vpRT.anchorMin = iconRT.anchorMin;
		vpRT.anchorMax = iconRT.anchorMax;
		vpRT.pivot = iconRT.pivot;
		vpRT.anchoredPosition = _origIconAnchoredPos;
		vpRT.sizeDelta = new Vector2(iconWidth, _iconHeight);
		_viewport = vpRT;

		// _iconImage를 IconViewport 자식으로 이동
		iconRT.SetParent(vpRT, false);
		iconRT.anchoredPosition = Vector2.zero;

		// 원본 아이콘 숨김, 이름은 스핀 중에도 표시
		_iconImage.enabled = false;
		if (_nameText != null) _nameText.text = "";
		if (_descText != null) _descText.enabled = false;

		// 스트립 컨테이너 생성
		GameObject stripGo = new GameObject("Strip", typeof(RectTransform));
		_strip = stripGo.GetComponent<RectTransform>();
		_strip.SetParent(_viewport, false);
		_strip.anchorMin = new Vector2(0, 0);
		_strip.anchorMax = new Vector2(1, 0);
		_strip.pivot = new Vector2(0.5f, 0f);
		_strip.anchoredPosition = Vector2.zero;
		_strip.sizeDelta = new Vector2(0, _iconHeight * STRIP_COUNT);

		// 스트립 아이콘 생성
		_stripIcons.Clear();
		_stripInfos.Clear();
		UpgradeInfo[] all = UpgradeDatabase.GetAll();

		for (int i = 0; i < STRIP_COUNT; i++)
		{
			GameObject iconGo = new GameObject($"Icon_{i}", typeof(RectTransform), typeof(Image));
			RectTransform rt = iconGo.GetComponent<RectTransform>();
			rt.SetParent(_strip, false);
			rt.anchorMin = new Vector2(0.5f, 0f);
			rt.anchorMax = new Vector2(0.5f, 0f);
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.sizeDelta = new Vector2(iconWidth, _iconHeight);
			// 아이콘 배치: 0번이 맨 아래, STRIP_COUNT-1이 맨 위
			rt.anchoredPosition = new Vector2(0, _iconHeight * i + _iconHeight * 0.5f);

			Image img = iconGo.GetComponent<Image>();
			UpgradeInfo randInfo = all[Random.Range(0, all.Length)];
			img.sprite = randInfo.icon;
			img.preserveAspect = true;
			_stripIcons.Add(img);
			_stripInfos.Add(randInfo);
		}
	}

	/// <summary>
	/// 수직 스크롤 코루틴. 위에서 아래로 아이콘이 내려오는 슬롯머신 효과.
	/// </summary>
	private IEnumerator ScrollCoroutine()
	{
		UpgradeInfo[] all = UpgradeDatabase.GetAll();
		float speed = 3000f;
		// 뷰포트 = 아이콘 크기 (200×200), 중앙 y = 아이콘 높이의 절반
		float centerY = _iconHeight * 0.5f;

		// 마지막으로 사운드를 재생한 아이콘 인덱스 추적
		int lastSoundIcon = -1;

		while (true)
		{
			float dt = Time.unscaledDeltaTime;

			if (_stopping)
			{
				// 감속 없이 일정 속도 유지 — 즉시 최종 아이콘 배치
				if (!_finalIconPlaced)
				{
					int closestIdx = GetClosestIconToCenter(centerY);
					int nextIdx = (closestIdx + 1) % STRIP_COUNT;
					_stripIcons[nextIdx].sprite = _assigned.icon;
					_stripInfos[nextIdx] = _assigned;
					_finalIconPlaced = true;
				}

				// 최종 아이콘이 중앙 근처에 오면 스냅 정지
				if (_finalIconPlaced)
				{
					int assignedIdx = -1;
					for (int i = 0; i < _stripIcons.Count; i++)
					{
						if (_stripInfos[i] == _assigned) { assignedIdx = i; break; }
					}
					if (assignedIdx >= 0)
					{
						float iconWorldY = _stripIcons[assignedIdx].rectTransform.anchoredPosition.y
										  + _strip.anchoredPosition.y;
						float dist = Mathf.Abs(iconWorldY - centerY);
						if (dist < speed * dt * 2f)
						{
							_snapCoroutine = StartCoroutine(SnapToCenter(centerY));
							yield return _snapCoroutine;
							break;
						}
					}
				}
			}

			// 스트립 이동 (아래로 스크롤 = anchoredPosition.y 감소)
			Vector2 pos = _strip.anchoredPosition;
			pos.y -= speed * dt;
			_strip.anchoredPosition = pos;

			// 하단 밖으로 나간 아이콘을 상단으로 재배치
			for (int i = 0; i < _stripIcons.Count; i++)
			{
				RectTransform rt = _stripIcons[i].rectTransform;
				float worldY = rt.anchoredPosition.y + _strip.anchoredPosition.y;
				if (worldY < -_iconHeight * 0.5f)
				{
					// 최상단 위치 찾기
					float maxY = float.MinValue;
					for (int j = 0; j < _stripIcons.Count; j++)
					{
						if (j != i && _stripIcons[j].rectTransform.anchoredPosition.y > maxY)
							maxY = _stripIcons[j].rectTransform.anchoredPosition.y;
					}
					Vector2 iconPos = rt.anchoredPosition;
					iconPos.y = maxY + _iconHeight;
					rt.anchoredPosition = iconPos;

					// 새 랜덤 스프라이트 (정지 중이고 최종 아이콘이 배치됐으면 유지)
					if (!_finalIconPlaced)
					{
						UpgradeInfo randInfo = all[Random.Range(0, all.Length)];
						_stripIcons[i].sprite = randInfo.icon;
						_stripInfos[i] = randInfo;
					}
				}
			}

			// 중앙을 지나는 아이콘 감지 → 사운드 재생
			int centerIcon = GetClosestIconToCenter(centerY);
			if (centerIcon != lastSoundIcon)
			{
				lastSoundIcon = centerIcon;
				AudioManager.Instance?.PlayUpgradeSlotSpin();
				if (_nameText != null)
					_nameText.text = _stripInfos[centerIcon].name;
			}

			yield return null;
		}

		// 스크롤 완료
		_spinCoroutine = null;
		CleanupStrip();
		Display(_assigned);
		AudioManager.Instance?.PlayUpgradeSlotConfirm();
		StartCoroutine(PunchScale());
	}

	/// <summary>
	/// 중앙에 가장 가까운 아이콘 인덱스를 반환.
	/// </summary>
	private int GetClosestIconToCenter(float centerY)
	{
		int closest = 0;
		float minDist = float.MaxValue;
		for (int i = 0; i < _stripIcons.Count; i++)
		{
			float worldY = _stripIcons[i].rectTransform.anchoredPosition.y + _strip.anchoredPosition.y;
			float dist = Mathf.Abs(worldY - centerY);
			if (dist < minDist)
			{
				minDist = dist;
				closest = i;
			}
		}
		return closest;
	}

	/// <summary>
	/// 최종 아이콘을 뷰포트 중앙에 정확히 스냅.
	/// </summary>
	private IEnumerator SnapToCenter(float centerY)
	{
		// 최종 아이콘(assigned.icon)을 가진 아이콘 찾기
		int targetIdx = -1;
		for (int i = 0; i < _stripIcons.Count; i++)
		{
			if (_stripIcons[i].sprite == _assigned.icon)
			{
				targetIdx = i;
				break;
			}
		}
		if (targetIdx < 0) targetIdx = GetClosestIconToCenter(centerY);

		float targetWorldY = centerY;
		float currentIconLocalY = _stripIcons[targetIdx].rectTransform.anchoredPosition.y;
		float targetStripY = targetWorldY - currentIconLocalY;

		Vector2 startPos = _strip.anchoredPosition;
		Vector2 endPos = new Vector2(startPos.x, targetStripY);

		float duration = 0.15f;
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f); // ease-out quad
			_strip.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
			yield return null;
		}
		_strip.anchoredPosition = endPos;
		_snapCoroutine = null;
	}

	/// <summary>
	/// 스트립 오브젝트를 제거하고 원본 UI를 복원.
	/// </summary>
	private void CleanupStrip()
	{
		if (_strip != null)
		{
			Destroy(_strip.gameObject);
			_strip = null;
		}
		_stripIcons.Clear();
		_stripInfos.Clear();

		// _iconImage를 원래 부모로 복귀
		if (_origIconParent != null && _iconImage != null)
		{
			_iconImage.rectTransform.SetParent(_origIconParent, false);
			_iconImage.rectTransform.anchoredPosition = _origIconAnchoredPos;
		}

		// IconViewport 파괴
		if (_iconViewportGo != null)
		{
			Destroy(_iconViewportGo);
			_iconViewportGo = null;
		}
		_viewport = null;

		// 원본 UI 복원
		_iconImage.enabled = true;
		if (_nameText != null) _nameText.enabled = true;
		if (_descText != null) _descText.enabled = true;
	}

	private IEnumerator PunchScale()
	{
		transform.localScale = Vector3.one * 1.15f;
		float t = 0f;
		while (t < 0.2f)
		{
			t += Time.unscaledDeltaTime;
			transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t / 0.2f);
			yield return null;
		}
		transform.localScale = Vector3.one;
	}

	private void Display(UpgradeInfo info)
	{
		if (_nameText != null)
			_nameText.text = info.name;
		if (_iconImage != null)
			_iconImage.sprite = info.icon;
		if (_descText != null)
			_descText.text = info.description;
	}
}
