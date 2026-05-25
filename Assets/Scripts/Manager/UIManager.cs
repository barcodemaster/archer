using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject _joystickLayer;
    [SerializeField] private GameObject _levelUpLayer;
    [SerializeField] private GameObject _pauseLayer;

    /// <summary>
    /// 레벨업 UI를 표시하고 조이스틱을 숨긴다.
    /// </summary>
    public void ShowLevelUp()
    {
        if (_joystickLayer != null)
        {
            UI_Joystick joystick = _joystickLayer.GetComponentInChildren<UI_Joystick>();
            if (joystick != null)
                joystick.ForceReset();
            _joystickLayer.SetActive(false);
        }
        if (_levelUpLayer != null) _levelUpLayer.SetActive(true);

    }

    /// <summary>
    /// EventSystem을 1프레임 disable하여 포인터 상태(pressed/dragged/hovered)를 완전 리셋한다.
    /// </summary>
    public void ResetEventSystem()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.enabled = false;
            StartCoroutine(ReenableEventSystem(eventSystem));
        }
    }

    private System.Collections.IEnumerator ReenableEventSystem(
        UnityEngine.EventSystems.EventSystem es)
    {
        yield return null;
        if (es != null) es.enabled = true;
    }

    /// <summary>
    /// 레벨업 UI를 숨기고 조이스틱을 복원한다.
    /// </summary>
    public void HideLevelUp()
    {
        if (_levelUpLayer != null) _levelUpLayer.SetActive(false);
        if (_joystickLayer != null) _joystickLayer.SetActive(true);
    }

    /// <summary>
    /// 일시정지 UI를 표시하고 조이스틱을 숨긴다.
    /// </summary>
    public void ShowPause()
    {
        if (_joystickLayer != null) _joystickLayer.SetActive(false);
    }

    /// <summary>
    /// 일시정지 UI를 숨기고 조이스틱을 복원한다.
    /// </summary>
    public void HidePause()
    {
        if (_joystickLayer != null) _joystickLayer.SetActive(true);
    }
}
