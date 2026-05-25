using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static Define;

public class UI_Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
	[SerializeField]
	private GameObject _background;

	[SerializeField]
	private GameObject _cursor;

	private Vector2 _cursorStartPos;
	private Vector2 _backgroundStartPos;

	private float _radius;
	private Vector2 _touchPos;

	public void Start()
	{
		_cursorStartPos = _cursor.transform.position;
		_backgroundStartPos = _background.transform.position;
		_radius = _background.GetComponent<RectTransform>().sizeDelta.y / 3;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_background.transform.position = eventData.position;
		_cursor.transform.position = eventData.position;
		_touchPos = eventData.position;

		//Debug.Log("OnPointerDown");
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_cursor.transform.position = _cursorStartPos;
		_background.transform.position = _backgroundStartPos;

		GameManager.Instance.JoystickDir = Vector2.zero;

		//Debug.Log("OnPointerUp");
	}

	/// <summary>
	/// 외부에서 강제로 조이스틱 상태를 리셋한다 (레벨업 등 UI 전환 시 사용).
	/// </summary>
	public void ForceReset()
	{
		_cursor.transform.position = _cursorStartPos;
		_background.transform.position = _backgroundStartPos;
		GameManager.Instance.JoystickDir = Vector2.zero;
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 touchDir = (eventData.position - _touchPos);

		float moveDist = Mathf.Min(touchDir.magnitude, _radius);
		Vector2 moveDir = touchDir.normalized;
		Vector2 newPosition = _touchPos + moveDir * moveDist;
		_cursor.transform.position = newPosition;

		GameManager.Instance.JoystickDir = moveDir;

		//Debug.Log("OnDrag");
	}
}
