using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Transform _target;

    /// <summary>플레이어 기준 카메라 오프셋 (탑뷰: Y값을 높게)</summary>
    [SerializeField] private Vector3 _offset = new Vector3(0f, 45f, -40f);

    /// <summary>카메라 회전각 (탑뷰: X를 90으로)</summary>
    [SerializeField] private Vector3 _rotation = new Vector3(90f, 0f, 0f);

    /// <summary>X축을 맵 중앙에 고정할지 여부</summary>
    [SerializeField] private bool _useFixedX = true;
    private float _fixedX;

    private float _shakeTimer;
    private float _shakeMagnitude;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// TileMap 경계에서 X축 중앙값을 계산하여 고정 좌표로 설정한다.
    /// </summary>
    public void SetMapCenter(TileMap tileMap)
    {
        if (tileMap == null) return;
        tileMap.GetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ);
        _fixedX = (minX + maxX) / 2f;
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
            if (PlayerController.Instance != null)
                _target = PlayerController.Instance.transform;
            return;
        }

        Vector3 pos = _target.position + _offset;
        if (_useFixedX)
            pos.x = _fixedX + _offset.x;
        transform.position = pos;

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

        transform.rotation = Quaternion.Euler(_rotation);
    }
}
