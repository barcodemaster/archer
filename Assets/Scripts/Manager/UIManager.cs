using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject _joystickLayer;
    [SerializeField] private GameObject _levelUpLayer;
    [SerializeField] private GameObject _pauseLayer;
    [SerializeField] private GameObject _stageProgressLayer;
    [SerializeField] private GameObject _equipmentLayer;

    private Image _fadeImage;

    private void Start()
    {
        CreateFadeImage();
    }

    /// <summary>
    /// Canvas 하위에 검은 fullscreen Image를 동적 생성한다.
    /// </summary>
    private void CreateFadeImage()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("FadeOverlay");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        _fadeImage = go.AddComponent<Image>();
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 화면을 어둡게 페이드 아웃한다 (alpha 0→1).
    /// </summary>
    public Coroutine FadeOut(float duration = 0.5f)
    {
        return StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }

    /// <summary>
    /// 화면을 밝게 페이드 인한다 (alpha 1→0).
    /// </summary>
    public Coroutine FadeIn(float duration = 0.5f)
    {
        return StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }

    private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
    {
        if (_fadeImage == null) yield break;

        _fadeImage.raycastTarget = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            _fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        _fadeImage.color = new Color(0f, 0f, 0f, toAlpha);

        if (toAlpha == 0f)
            _fadeImage.raycastTarget = false;
    }

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

    /// <summary>
    /// 스테이지 진행 패널을 표시한다.
    /// </summary>
    public void ShowStageProgress()
    {
        if (_stageProgressLayer != null) _stageProgressLayer.SetActive(true);
    }

    /// <summary>
    /// 스테이지 진행 패널을 숨긴다.
    /// </summary>
    public void HideStageProgress()
    {
        if (_stageProgressLayer != null) _stageProgressLayer.SetActive(false);
    }

    /// <summary>
    /// 장비창을 표시하고 게임을 일시정지한다.
    /// </summary>
    public void ShowEquipment()
    {
        if (_joystickLayer != null)
        {
            UI_Joystick joystick = _joystickLayer.GetComponentInChildren<UI_Joystick>();
            if (joystick != null)
                joystick.ForceReset();
            _joystickLayer.SetActive(false);
        }
        if (_equipmentLayer != null)
        {
            _equipmentLayer.SetActive(true);
            var panel = _equipmentLayer.GetComponent<UI_EquipmentPanel>();
            if (panel != null) panel.Open();
        }
        Time.timeScale = 0f;
        GameManager.Instance.IsPaused = true;
    }

    /// <summary>
    /// 장비창을 숨기고 게임을 재개한다.
    /// </summary>
    public void HideEquipment()
    {
        if (_equipmentLayer != null)
        {
            var panel = _equipmentLayer.GetComponent<UI_EquipmentPanel>();
            if (panel != null) panel.Close();
            _equipmentLayer.SetActive(false);
        }
        if (_joystickLayer != null) _joystickLayer.SetActive(true);
        Time.timeScale = 1f;
        GameManager.Instance.IsPaused = false;
    }
}
