using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_StageProgress : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private Slider _progressSlider2;

    [Header("Stage Texts")]
    [SerializeField] private TextMeshProUGUI _currentStageText;
    [SerializeField] private TextMeshProUGUI _nextStageText;
    [SerializeField] private TextMeshProUGUI _nextNextStageText;
    [SerializeField] private TextMeshProUGUI _totalText;
    [SerializeField] private TextMeshProUGUI _ellipsisText;

    [Header("Timing")]
    [SerializeField] private float _sliderFillDuration = 1.0f;
    [SerializeField] private float _punchScaleSize = 1.4f;
    [SerializeField] private float _punchScaleDuration = 0.3f;
    [SerializeField] private float _autoCloseDelay = 1.0f;
    [SerializeField] private float _panelShowDelay = 1.0f;

    private bool _isNearEnd;
    private StageManager _stageManager;

    private void Start()
    {
        _stageManager = StageManager.Instance;
        _stageManager.OnExitDoorOpened += OnExitDoorOpened;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
            _stageManager.OnExitDoorOpened -= OnExitDoorOpened;
    }

    private void OnExitDoorOpened()
    {
        if (StageManager.Instance.IsStartStage(StageManager.Instance.CurrentStageIndex)) return;

        gameObject.SetActive(true);
        int current = StageManager.Instance.CurrentStageIndex;
        int total = StageManager.Instance.TotalStageCount - 1;

        if (current >= total) return;

        PopulateStageNumbers(current, total);
        StartCoroutine(StageProgressSequence());
    }

    /// <summary>
    /// 스테이지 번호와 이정표 텍스트를 채운다.
    /// </summary>
    private void PopulateStageNumbers(int current, int total)
    {
        _isNearEnd = (current + 2) > total;

        int pos1, pos2, pos3;

        if (_isNearEnd)
        {
            pos1 = current - 1;
            pos2 = current;
            pos3 = current + 1;
            _progressSlider.value = 1f;
            _progressSlider2.value = 0f;
        }
        else
        {
            pos1 = current;
            pos2 = current + 1;
            pos3 = current + 2;
            _progressSlider.value = 0f;
            _progressSlider2.value = 0f;
        }

        _currentStageText.text = pos1.ToString();
        _nextStageText.text = pos2.ToString();
        _nextNextStageText.text = pos3.ToString();

        // 5단위 이정표 계산
        int milestone = ((pos3 / 5) + 1) * 5;
        bool showMilestone = milestone > pos3 && milestone <= total;

        if (showMilestone)
        {
            _totalText.text = $"{milestone}/{total}";
        }

        if (_totalText != null)
            _totalText.gameObject.SetActive(showMilestone);
        if (_ellipsisText != null)
            _ellipsisText.gameObject.SetActive(showMilestone);
    }

    /// <summary>
    /// 패널 표시 -> 슬라이더 채우기 -> 펀치스케일 -> 자동 닫기 시퀀스.
    /// </summary>
    private IEnumerator StageProgressSequence()
    {
        // 시작 시 모든 텍스트 스케일 초기화
        ResetTextScales();

        yield return new WaitForSeconds(_panelShowDelay);

        AudioManager.Instance?.PlayStageProgress();
        UIManager.Instance.ShowStageProgress();

        // 애니메이션 대상 슬라이더 결정
        Slider targetSlider = _isNearEnd ? _progressSlider2 : _progressSlider;

        // 슬라이더 0 -> 1 ease-out 채우기
        float elapsed = 0f;
        while (elapsed < _sliderFillDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _sliderFillDuration);
            float eased = 1f - (1f - t) * (1f - t);
            targetSlider.value = eased;
            yield return null;
        }
        targetSlider.value = 1f;

        // 펀치스케일 대상: 텍스트에 적용
        Transform punchTarget = _isNearEnd ? _nextNextStageText.transform : _nextStageText.transform;
        Vector3 originalScale = punchTarget.localScale;
        Vector3 punchScale = originalScale * _punchScaleSize;

        // scale up (30% 시간)
        float upDuration = _punchScaleDuration * 0.3f;
        elapsed = 0f;
        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / upDuration);
            punchTarget.localScale = Vector3.Lerp(originalScale, punchScale, t);
            yield return null;
        }

        // scale down (70% 시간)
        float downDuration = _punchScaleDuration * 0.7f;
        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / downDuration);
            punchTarget.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }
        punchTarget.localScale = originalScale;

        yield return new WaitForSeconds(_autoCloseDelay);

        UIManager.Instance.HideStageProgress();
    }

    /// <summary>
    /// 모든 스테이지 텍스트의 스케일을 1로 초기화한다.
    /// </summary>
    private void ResetTextScales()
    {
        if (_currentStageText != null) _currentStageText.transform.localScale = Vector3.one;
        if (_nextStageText != null) _nextStageText.transform.localScale = Vector3.one;
        if (_nextNextStageText != null) _nextNextStageText.transform.localScale = Vector3.one;
    }
}
