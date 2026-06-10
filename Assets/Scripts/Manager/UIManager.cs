using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject _joystickLayer;
    [SerializeField] private GameObject _levelUpLayer;
    [SerializeField] private GameObject _pauseLayer;
    [SerializeField] private GameObject _stageProgressLayer;
    [SerializeField] private GameObject _equipmentLayer;
    [SerializeField] private GameObject _gameOverLayer;
    [SerializeField] private GameObject _aliveLayer;
    [SerializeField] private GameObject _angelLayer;

    private Image _fadeImage;
    private UI_PausePanel _pausePanel;

    private void Start()
    {
        CreateFadeImage();
        _pausePanel = FindAnyObjectByType<UI_PausePanel>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            OnBackButtonPressed();
    }

    /// <summary>
    /// Android 뒤로가기 버튼 또는 ESC 키 입력 처리.
    /// </summary>
    private void OnBackButtonPressed()
    {
        if (_equipmentLayer != null && _equipmentLayer.activeSelf)
        {
            HideEquipment();
            return;
        }

        if (_levelUpLayer != null && _levelUpLayer.activeSelf)
            return;

        if (_angelLayer != null && _angelLayer.activeSelf)
            return;

        if (_aliveLayer != null && _aliveLayer.activeSelf)
            return;

        if (_gameOverLayer != null && _gameOverLayer.activeSelf)
            return;

        if (GameManager.Instance.IsPaused)
        {
            var pausePanel = _pausePanel;
            if (pausePanel != null)
                pausePanel.OnResumeClicked();
        }
        else
        {
            var pausePanel = _pausePanel;
            if (pausePanel != null)
                pausePanel.OnPauseClicked();
        }
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

        _fadeImage.raycastTarget = (toAlpha > 0f);
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

    public void ShowJoystick()
    {
        if(_joystickLayer != null) _joystickLayer.SetActive(true);
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
	/// 부활 패널을 표시한다.
	/// </summary>
    public void ShowAlive()
    {
        if(_joystickLayer != null) _joystickLayer.SetActive(false);
        if(_aliveLayer != null)
        {
            var panel = _aliveLayer.GetComponent<UI_AlivePanel>();
            if (panel != null)
            { 
                panel.Show();
                _aliveLayer.SetActive(true);
            }
		}
	}

	/// <summary>
	/// 게임오버 패널을 표시한다.
	/// </summary>
	public void ShowGameOver(int stageIndex, int gold)
    {
        if (_joystickLayer != null) _joystickLayer.SetActive(false);
        if (_gameOverLayer != null)
        {
            var panel = _gameOverLayer.GetComponent<UI_GameOverPanel>();
            if (panel != null)
                panel.Show(stageIndex, gold);
            else
                _gameOverLayer.SetActive(true);
        }
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

    /// <summary>
    /// 천사 축복 패널을 표시하고 조이스틱을 숨긴다.
    /// </summary>
    public void ShowAngelPanel(AngelNPC angel = null)
    {
        if (_joystickLayer != null)
        {
            UI_Joystick joystick = _joystickLayer.GetComponentInChildren<UI_Joystick>();
            if (joystick != null)
                joystick.ForceReset();
            _joystickLayer.SetActive(false);
        }
        if (_angelLayer != null)
        {
            var panel = _angelLayer.GetComponent<UI_AngelPanel>();
            if (panel != null) panel.Show(angel);
        }
    }

    /// <summary>
    /// 천사 축복 패널을 숨기고 조이스틱을 복원한다.
    /// </summary>
    public void HideAngelPanel()
    {
        if (_angelLayer != null) _angelLayer.SetActive(false);
        if (_joystickLayer != null) _joystickLayer.SetActive(true);
        Time.timeScale = 1f;
        GameManager.Instance.IsPaused = false;
    }
}
