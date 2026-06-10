using UnityEngine;
using UnityEngine.EventSystems;

public class UI_Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
	[SerializeField]
	private GameObject _background;

	[SerializeField]
	private GameObject _cursor;

	private RectTransform _cursorRt;
	private RectTransform _backgroundRt;
	private RectTransform _parentRt;
	private Camera _uiCamera;

	private Vector2 _cursorStartPos;
	private Vector2 _backgroundStartPos;

	private float _radius;
	private Vector2 _touchPos; // 로컬 좌표 기준 터치 시작 위치

	private void Start()
	{
		_cursorRt = _cursor.GetComponent<RectTransform>();
		_backgroundRt = _background.GetComponent<RectTransform>();
		_cursorStartPos = _cursorRt.anchoredPosition;
		_backgroundStartPos = _backgroundRt.anchoredPosition;
		_radius = _backgroundRt.sizeDelta.y / 5;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		// 터치 위치로 조이스틱 이동 (팔로우 방식)
		_background.transform.position = eventData.position;
		_cursor.transform.position = eventData.position;
		_touchPos = eventData.position;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		// 원래 위치로 복귀
		_cursorRt.anchoredPosition = _cursorStartPos;
		_backgroundRt.anchoredPosition = _backgroundStartPos;
		GameManager.Instance.JoystickDir = Vector2.zero;
	}

	/// <summary>
	/// 외부에서 강제로 조이스틱 상태를 리셋한다 (레벨업 등 UI 전환 시 사용).
	/// </summary>
	public void ForceReset()
	{
		_cursorRt.anchoredPosition = _cursorStartPos;
		_backgroundRt.anchoredPosition = _backgroundStartPos;
		GameManager.Instance.JoystickDir = Vector2.zero;
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 touchDir = (eventData.position - _touchPos);
		float moveDist = Mathf.Min(touchDir.magnitude, _radius);
		Vector2 moveDir = touchDir.normalized;
		Vector2 newPosition = _touchPos + moveDir * moveDist;
		_cursor.transform.position = newPosition;

		GameManager.Instance.JoystickDir = moveDir;  // 글로벌 입력 전달
	}
}
