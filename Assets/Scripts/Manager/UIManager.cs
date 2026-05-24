using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject _joystickLayer;
    [SerializeField] private GameObject _levelUpLayer;

    /// <summary>
    /// 레벨업 UI를 표시하고 조이스틱을 숨긴다.
    /// </summary>
    public void ShowLevelUp()
    {
        if (_joystickLayer != null) _joystickLayer.SetActive(false);
        if (_levelUpLayer != null) _levelUpLayer.SetActive(true);
    }

    /// <summary>
    /// 레벨업 UI를 숨기고 조이스틱을 복원한다.
    /// </summary>
    public void HideLevelUp()
    {
        if (_levelUpLayer != null) _levelUpLayer.SetActive(false);
        if (_joystickLayer != null) _joystickLayer.SetActive(true);
    }
}
