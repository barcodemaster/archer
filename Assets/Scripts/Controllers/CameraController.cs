using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;

    /// <summary>플레이어 기준 카메라 오프셋 (탑뷰: Y값을 높게)</summary>
    [SerializeField] private Vector3 _offset = new Vector3(0f, 10f, 0f);

    /// <summary>카메라 회전각 (탑뷰: X를 90으로)</summary>
    [SerializeField] private Vector3 _rotation = new Vector3(90f, 0f, 0f);

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
        transform.LookAt(_target);
    }
}
