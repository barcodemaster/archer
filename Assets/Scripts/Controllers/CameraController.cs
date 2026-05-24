using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Transform _target;

    /// <summary>플레이어 기준 카메라 오프셋 (탑뷰: Y값을 높게)</summary>
    [SerializeField] private Vector3 _offset = new Vector3(0f, 10f, 0f);

    /// <summary>카메라 회전각 (탑뷰: X를 90으로)</summary>
    [SerializeField] private Vector3 _rotation = new Vector3(90f, 0f, 0f);

    private float _shakeTimer;
    private float _shakeMagnitude;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 지정한 시간 동안 카메라를 흔든다.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        _shakeTimer = duration;
        _shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (_target == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                _target = player.transform;
            return;
        }

        transform.position = _target.position + _offset;

        if (_shakeTimer > 0f)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                _shakeTimer = 0f;
            }
            else
            {
                transform.position += Random.insideUnitSphere * _shakeMagnitude;
                _shakeTimer -= Time.deltaTime;
            }
        }

        transform.LookAt(_target);
    }
}
